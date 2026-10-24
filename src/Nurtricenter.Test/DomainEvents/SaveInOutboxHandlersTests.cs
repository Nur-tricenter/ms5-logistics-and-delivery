namespace Nurtricenter.Test.DomainEvents;

using FluentAssertions;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using Nurtricenter.Application.DomainEvents;
using Nurtricenter.Core.Domain.Courier.Enums;
using Nurtricenter.Core.Domain.Courier.Events;
using Nurtricenter.Core.Domain.Delivery.Events;
using Nurtricenter.Core.Domain.Route.Events;

public class SaveInOutboxHandlersTests
{
    private sealed class CapturingOutboxService : IOutboxService<DomainEvent>
    {
        public OutboxMessage<DomainEvent>? Added { get; private set; }

        public Task AddAsync(OutboxMessage<DomainEvent> message)
        {
            Added = message;
            return Task.CompletedTask;
        }
    }

    public static TheoryData<DomainEvent, Type> Cases()
    {
        var courierId = Guid.NewGuid();
        var routeId = Guid.NewGuid();
        var deliveryId = Guid.NewGuid();

        return new TheoryData<DomainEvent, Type>
        {
            { new CourierCreatedEvent(courierId, "Jane Doe", CourierStatus.Available), typeof(SaveInOutboxWhenCourierCreatedEvent) },
            { new CourierStatusChangedEvent(courierId, CourierStatus.Available, CourierStatus.OnRoute), typeof(SaveInOutboxWhenCourierStatusChangedEvent) },
            { new RouteCreatedEvent(routeId, courierId, new DateOnly(2026, 10, 21)), typeof(SaveInOutboxWhenRouteCreatedEvent) },
            { new CourierAssignedToRouteEvent(routeId, courierId), typeof(SaveInOutboxWhenCourierAssignedToRouteEvent) },
            { new DeliveryAddedToRouteEvent(routeId, deliveryId, "package-1", "patient-1"), typeof(SaveInOutboxWhenDeliveryAddedToRouteEvent) },
            { new RouteStartedEvent(routeId), typeof(SaveInOutboxWhenRouteStartedEvent) },
            { new RouteCompletedEvent(routeId), typeof(SaveInOutboxWhenRouteCompletedEvent) },
            { new RouteCancelledEvent(routeId), typeof(SaveInOutboxWhenRouteCancelledEvent) },
            { new DeliveryCompletedEvent(deliveryId, routeId, DateTime.UtcNow), typeof(SaveInOutboxWhenDeliveryCompletedEvent) },
            { new DeliveryFailedEvent(deliveryId, routeId, "recipient absent"), typeof(SaveInOutboxWhenDeliveryFailedEvent) }
        };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Handler_StagesTheDomainEventAsOutboxContent(DomainEvent domainEvent, Type handlerType)
    {
        var outboxService = new CapturingOutboxService();
        dynamic handler = Activator.CreateInstance(handlerType, outboxService)!;

        await handler.Handle((dynamic)domainEvent, CancellationToken.None);

        outboxService.Added.Should().NotBeNull();
        outboxService.Added!.Content.Should().BeSameAs(domainEvent);
    }
}
