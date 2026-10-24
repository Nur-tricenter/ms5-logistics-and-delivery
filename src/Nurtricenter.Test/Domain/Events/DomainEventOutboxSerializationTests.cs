namespace Nurtricenter.Test.Domain.Events;

using FluentAssertions;
using Joseco.DDD.Core.Abstractions;
using Newtonsoft.Json;
using Nurtricenter.Core.Domain.Courier.Enums;
using Nurtricenter.Core.Domain.Courier.Events;
using Nurtricenter.Core.Domain.Delivery.Events;
using Nurtricenter.Core.Domain.Route.Events;

/// <summary>
/// The outbox persists <c>OutboxMessage&lt;DomainEvent&gt;.Content</c> as JSON with Newtonsoft's
/// <c>TypeNameHandling.All</c>. Every domain event must round-trip with its payload intact, and
/// none may expose a public parameterless constructor (which would make Newtonsoft build the
/// event through it and skip every get-only payload property).
/// </summary>
public class DomainEventOutboxSerializationTests
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        TypeNameHandling = TypeNameHandling.All
    };

    public static TheoryData<DomainEvent> Events()
    {
        var courierId = Guid.NewGuid();
        var routeId = Guid.NewGuid();
        var deliveryId = Guid.NewGuid();

        return new TheoryData<DomainEvent>
        {
            new CourierCreatedEvent(courierId, "Jane Doe", CourierStatus.Available),
            new CourierStatusChangedEvent(courierId, CourierStatus.Available, CourierStatus.OnRoute),
            new RouteCreatedEvent(routeId, courierId, new DateOnly(2026, 10, 21)),
            new CourierAssignedToRouteEvent(routeId, courierId),
            new DeliveryAddedToRouteEvent(routeId, deliveryId, "package-1", "patient-1"),
            new RouteStartedEvent(routeId),
            new RouteCompletedEvent(routeId),
            new RouteCancelledEvent(routeId),
            new DeliveryCompletedEvent(deliveryId, routeId, new DateTime(2026, 10, 21, 10, 30, 0, DateTimeKind.Utc)),
            new DeliveryFailedEvent(deliveryId, routeId, "recipient absent")
        };
    }

    [Theory]
    [MemberData(nameof(Events))]
    public void Event_RoundTripsThroughTheOutboxSerializer_WithPayloadIntact(DomainEvent domainEvent)
    {
        var json = JsonConvert.SerializeObject(domainEvent, Settings);

        var deserialized = JsonConvert.DeserializeObject<DomainEvent>(json, Settings);

        deserialized.Should().BeOfType(domainEvent.GetType());
        deserialized.Should().BeEquivalentTo(domainEvent);
    }

    [Fact]
    public void EveryDomainEvent_DoesNotExposeAPublicParameterlessConstructor()
    {
        var domainEventTypes = typeof(CourierCreatedEvent).Assembly.GetTypes()
            .Where(t => !t.IsAbstract
                        && typeof(DomainEvent).IsAssignableFrom(t)
                        && (t.Namespace?.StartsWith("Nurtricenter.Core.Domain") ?? false))
            .ToList();

        domainEventTypes.Should().NotBeEmpty();

        foreach (var type in domainEventTypes)
        {
            var parameterless = type.GetConstructor(Type.EmptyTypes);
            parameterless.Should().BeNull(
                $"domain event {type.Name} must not expose a parameterless constructor");
        }
    }
}
