namespace Nurtricenter.Application.DomainEvents;

using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using MediatR;
using Nurtricenter.Core.Domain.Delivery.Events;

/// <summary>
/// Stages <see cref="DeliveryFailedEvent"/> in the transactional outbox, inside the same
/// database transaction as the aggregate change that raised it.
/// </summary>
public sealed class SaveInOutboxWhenDeliveryFailedEvent : INotificationHandler<DeliveryFailedEvent>
{
    private readonly IOutboxService<DomainEvent> _outboxService;

    public SaveInOutboxWhenDeliveryFailedEvent(IOutboxService<DomainEvent> outboxService)
    {
        _outboxService = outboxService;
    }

    public async Task Handle(DeliveryFailedEvent notification, CancellationToken cancellationToken = default)
    {
        await _outboxService.AddAsync(new OutboxMessage<DomainEvent>(notification));
    }
}
