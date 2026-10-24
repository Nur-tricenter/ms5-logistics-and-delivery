namespace Nurtricenter.Core.Domain.Courier.Events;

using Joseco.DDD.Core.Abstractions;
using Nurtricenter.Core.Domain.Courier.Enums;

/// <summary>
/// Single explicit constructor on purpose: a public parameterless constructor would make the
/// outbox's Newtonsoft deserializer build the event through it and skip every get-only payload
/// property, so the event would silently arrive empty.
/// </summary>
public sealed record CourierCreatedEvent : DomainEvent
{
    public Guid CourierId { get; }
    public string FullName { get; }
    public CourierStatus Status { get; }

    public CourierCreatedEvent(Guid courierId, string fullName, CourierStatus status)
    {
        OccuredOn = DateTime.UtcNow;
        CourierId = courierId;
        FullName = fullName;
        Status = status;
    }
}
