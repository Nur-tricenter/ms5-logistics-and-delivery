namespace Nurtricenter.Core.Domain.Route.Events;

using Joseco.DDD.Core.Abstractions;

/// <summary>
/// Single explicit constructor on purpose: a public parameterless constructor would make the
/// outbox's Newtonsoft deserializer build the event through it and skip every get-only payload
/// property, so the event would silently arrive empty.
/// </summary>
public sealed record RouteCompletedEvent : DomainEvent
{
    public Guid RouteId { get; }

    public RouteCompletedEvent(Guid routeId)
    {
        OccuredOn = DateTime.UtcNow;
        RouteId = routeId;
    }
}
