namespace Nurtricenter.Infrastructure.Data;

using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.EFCore.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Persists aggregates and the transactional outbox in a single database transaction.
/// </summary>
/// <remarks>
/// Domain events are drained and published BEFORE <c>SaveChangesAsync</c> on purpose: the
/// in-process <c>SaveInOutboxWhen*</c> handlers stage outbox rows into this same context, so
/// aggregates and outbox rows are saved atomically. The worker later reads the outbox and
/// publishes the integration events, so a crashed request can never lose an event that "succeeded".
/// <para>
/// The <see cref="_isCommitting"/> guard makes a nested commit a no-op. The route-completion
/// handler calls <see cref="CommitAsync"/> while an outer commit is draining events; its mutations
/// are persisted by the outer save instead.
/// </para>
/// </remarks>
public sealed class UnitOfWork(AppDbContext context, IMediator mediator) : IUnitOfWork, IOutboxDatabase<DomainEvent>
{
    private const int MaxDrainRounds = 10;

    private bool _isCommitting;

    public DbSet<OutboxMessage<DomainEvent>> GetOutboxMessages() => context.OutboxMessages;

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_isCommitting)
            return;

        _isCommitting = true;

        try
        {
            await DrainDomainEventsAsync(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            context.ChangeTracker.Clear();
            throw;
        }
        finally
        {
            _isCommitting = false;
        }
    }

    private async Task DrainDomainEventsAsync(CancellationToken cancellationToken)
    {
        for (var round = 0; round < MaxDrainRounds; round++)
        {
            var domainEvents = CollectDomainEvents();

            if (domainEvents.Length == 0)
                return;

            foreach (var domainEvent in domainEvents)
            {
                await mediator.Publish(domainEvent, cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Domain events kept being raised after {MaxDrainRounds} drain rounds, which indicates an event cycle.");
    }

    private DomainEvent[] CollectDomainEvents()
    {
        var collected = new List<DomainEvent>();

        foreach (var entity in context.ChangeTracker.Entries<Entity>().Select(entry => entry.Entity))
        {
            if (!entity.DomainEvents.Any())
                continue;

            collected.AddRange(entity.DomainEvents);
            entity.ClearDomainEvents();
        }

        return collected.ToArray();
    }
}
