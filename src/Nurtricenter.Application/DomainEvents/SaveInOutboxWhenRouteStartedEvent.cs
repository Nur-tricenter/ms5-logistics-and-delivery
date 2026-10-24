namespace Nurtricenter.Application.DomainEvents;

using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using MediatR;
using Nurtricenter.Core.Domain.Route.Events;

/// <summary>
/// Stages <see cref="RouteStartedEvent"/> in the transactional outbox, inside the same
/// database transaction as the aggregate change that raised it.
/// </summary>
public sealed class SaveInOutboxWhenRouteStartedEvent : INotificationHandler<RouteStartedEvent>
{
    private readonly IOutboxService<DomainEvent> _outboxService;

    public SaveInOutboxWhenRouteStartedEvent(IOutboxService<DomainEvent> outboxService)
    {
        _outboxService = outboxService;
    }

    public async Task Handle(RouteStartedEvent notification, CancellationToken cancellationToken = default)
    {
        await _outboxService.AddAsync(new OutboxMessage<DomainEvent>(notification));
    }
}
