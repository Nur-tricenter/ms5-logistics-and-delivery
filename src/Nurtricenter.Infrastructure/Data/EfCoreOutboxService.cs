namespace Nurtricenter.Infrastructure.Data;

using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using Joseco.Outbox.EFCore.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Persistence-boundary implementation of the transactional outbox contracts.
/// It stages messages in the change tracker of the <see cref="IOutboxDatabase{TEvent}"/>'s
/// <see cref="DbContext"/>; nothing is written until that same context is saved by
/// <see cref="UnitOfWork.CommitAsync"/>.
/// </summary>
/// <remarks>
/// This replaces <c>Joseco.Outbox.EFCore.Service.OutboxService&lt;TEvent&gt;</c>, which is
/// <c>internal</c> and therefore not registrable. The library's own
/// <c>AddOutbox&lt;TEvent&gt;()</c> entry point cannot be used either: it is compiled against
/// MediatR 12 and throws a <c>TypeLoadException</c> on MediatR 14.
/// </remarks>
public class EfCoreOutboxService<TEvent> : IOutboxService<TEvent>, IOutboxRepository<TEvent>
{
    private readonly IOutboxDatabase<TEvent> _outboxDatabase;

    public EfCoreOutboxService(IOutboxDatabase<TEvent> outboxDatabase)
    {
        _outboxDatabase = outboxDatabase;
    }

    public async Task AddAsync(OutboxMessage<TEvent> message)
    {
        await _outboxDatabase.GetOutboxMessages().AddAsync(message);
    }

    public Task Update(OutboxMessage<TEvent> message)
    {
        _outboxDatabase.GetOutboxMessages().Update(message);

        return Task.CompletedTask;
    }

    public async Task<IEnumerable<OutboxMessage<TEvent>>> GetUnprocessedAsync(int pageSize)
    {
        return await _outboxDatabase.GetOutboxMessages()
            .Where(message => !message.Processed)
            .OrderBy(message => message.Created)
            .Take(pageSize)
            .ToListAsync();
    }
}
