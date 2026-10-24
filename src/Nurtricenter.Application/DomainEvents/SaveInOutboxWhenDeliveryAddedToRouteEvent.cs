namespace Nurtricenter.Application.DomainEvents;

using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using MediatR;
using Nurtricenter.Core.Domain.Route.Events;

/// <summary>
/// Stages <see cref="DeliveryAddedToRouteEvent"/> in the transactional outbox, inside the same
/// database transaction as the aggregate change that raised it.
/// </summary>
public sealed class SaveInOutboxWhenDeliveryAddedToRouteEvent : INotificationHandler<DeliveryAddedToRouteEvent>
{
    private readonly IOutboxService<DomainEvent> _outboxService;

    public SaveInOutboxWhenDeliveryAddedToRouteEvent(IOutboxService<DomainEvent> outboxService)
    {
        _outboxService = outboxService;
    }

    public async Task Handle(DeliveryAddedToRouteEvent notification, CancellationToken cancellationToken = default)
    {
        await _outboxService.AddAsync(new OutboxMessage<DomainEvent>(notification));
    }
}
