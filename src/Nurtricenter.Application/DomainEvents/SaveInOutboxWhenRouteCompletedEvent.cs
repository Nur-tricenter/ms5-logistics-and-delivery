namespace Nurtricenter.Application.DomainEvents;

using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using MediatR;
using Nurtricenter.Core.Domain.Route.Events;

/// <summary>
/// Stages <see cref="RouteCompletedEvent"/> in the transactional outbox, inside the same
/// database transaction as the aggregate change that raised it.
/// </summary>
public sealed class SaveInOutboxWhenRouteCompletedEvent : INotificationHandler<RouteCompletedEvent>
{
    private readonly IOutboxService<DomainEvent> _outboxService;

    public SaveInOutboxWhenRouteCompletedEvent(IOutboxService<DomainEvent> outboxService)
    {
        _outboxService = outboxService;
    }

    public async Task Handle(RouteCompletedEvent notification, CancellationToken cancellationToken = default)
    {
        await _outboxService.AddAsync(new OutboxMessage<DomainEvent>(notification));
    }
}
