namespace Nurtricenter.Core.Domain.Route.Events;

using Joseco.DDD.Core.Abstractions;

/// <summary>
/// Single explicit constructor on purpose: a public parameterless constructor would make the
/// outbox's Newtonsoft deserializer build the event through it and skip every get-only payload
/// property, so the event would silently arrive empty.
/// </summary>
public sealed record DeliveryAddedToRouteEvent : DomainEvent
{
    public Guid RouteId { get; }
    public Guid DeliveryId { get; }
    public string PackageId { get; }
    public string PatientId { get; }

    public DeliveryAddedToRouteEvent(Guid routeId, Guid deliveryId, string packageId, string patientId)
    {
        OccuredOn = DateTime.UtcNow;
        RouteId = routeId;
        DeliveryId = deliveryId;
        PackageId = packageId;
        PatientId = patientId;
    }
}
