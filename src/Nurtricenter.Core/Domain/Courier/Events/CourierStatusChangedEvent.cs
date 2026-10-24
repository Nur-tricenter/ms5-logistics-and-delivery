namespace Nurtricenter.Core.Domain.Courier.Events;

using Joseco.DDD.Core.Abstractions;
using Nurtricenter.Core.Domain.Courier.Enums;

/// <summary>
/// Single explicit constructor on purpose: a public parameterless constructor would make the
/// outbox's Newtonsoft deserializer build the event through it and skip every get-only payload
/// property, so the event would silently arrive empty.
/// </summary>
public sealed record CourierStatusChangedEvent : DomainEvent
{
    public Guid CourierId { get; }
    public CourierStatus OldStatus { get; }
    public CourierStatus NewStatus { get; }

    public CourierStatusChangedEvent(Guid courierId, CourierStatus oldStatus, CourierStatus newStatus)
    {
        OccuredOn = DateTime.UtcNow;
        CourierId = courierId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }
}
