namespace Nurtricenter.Core.Domain.Delivery.Events;

using Joseco.DDD.Core.Abstractions;

/// <summary>
/// Single explicit constructor on purpose: a public parameterless constructor would make the
/// outbox's Newtonsoft deserializer build the event through it and skip every get-only payload
/// property, so the event would silently arrive empty.
/// </summary>
public sealed record DeliveryFailedEvent : DomainEvent
{
    public Guid DeliveryId { get; }
    public Guid RouteId { get; }
    public string Reason { get; }

    public DeliveryFailedEvent(Guid deliveryId, Guid routeId, string reason)
    {
        OccuredOn = DateTime.UtcNow;
        DeliveryId = deliveryId;
        RouteId = routeId;
        Reason = reason;
    }
}
