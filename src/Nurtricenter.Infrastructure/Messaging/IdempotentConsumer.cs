namespace Nurtricenter.Infrastructure.Messaging;

using Joseco.Communication.External.Contracts.Message;
using Joseco.Communication.External.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nurtricenter.Infrastructure.Data;

/// <summary>
/// Inbox decorator: RabbitMQ delivers at-least-once, so the same message can arrive twice. The
/// claim (an <see cref="InboxMessage"/> row keyed by message id) and the inner consumer's changes
/// land in ONE database transaction: either both are saved or neither is.
/// </summary>
/// <remarks>
/// <para>
/// The claim is staged in the scoped <see cref="AppDbContext"/> BEFORE the inner consumer runs. Inner
/// consumers commit through <c>UnitOfWork.CommitAsync</c> on that same context, so the inbox row is
/// saved together with the aggregate and its outbox rows. The final <c>SaveChangesAsync</c> only covers
/// an inner consumer that changed nothing (it is a no-op otherwise). If the inner consumer throws, the
/// unit of work clears the change tracker, the claim is discarded with everything else, and the broker
/// redelivers.
/// </para>
/// <para>
/// <typeparamref name="TInner"/> is the concrete consumer, not <see cref="IIntegrationMessageConsumer{T}"/>:
/// <c>AddRabbitMqConsumer</c> registers the decorated pipeline under that interface, so resolving the
/// inner one through it would be a circular dependency.
/// </para>
/// </remarks>
public sealed class IdempotentConsumer<T, TInner>(
    TInner inner,
    AppDbContext context,
    ILogger<IdempotentConsumer<T, TInner>> logger) : IIntegrationMessageConsumer<T>
    where T : IntegrationMessage
    where TInner : class, IIntegrationMessageConsumer<T>
{
    public async Task HandleAsync(T message, CancellationToken cancellationToken = default)
    {
        if (await context.InboxMessages.AsNoTracking().AnyAsync(m => m.MessageId == message.Id, cancellationToken))
        {
            logger.LogInformation("Skipping {MessageType} {MessageId}: already in the inbox.", typeof(T).Name, message.Id);
            return;
        }

        context.InboxMessages.Add(new InboxMessage(
            message.Id, typeof(T).Name, message.CorrelationId, message.CreatedAt, DateTimeOffset.UtcNow));

        try
        {
            await inner.HandleAsync(message, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            context.ChangeTracker.Clear();

            if (!await context.InboxMessages.AsNoTracking().AnyAsync(m => m.MessageId == message.Id, cancellationToken))
                throw;

            // A concurrent delivery of the same message won the primary-key race.
            logger.LogInformation("{MessageType} {MessageId} was claimed by a concurrent delivery; skipping.",
                typeof(T).Name, message.Id);
        }
    }
}
