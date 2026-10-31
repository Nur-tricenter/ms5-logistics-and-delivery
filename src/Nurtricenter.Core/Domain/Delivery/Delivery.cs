#pragma warning disable CS8618 // Private parameterless ctor for serialization

namespace Nurtricenter.Core.Domain.Delivery;

using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using Nurtricenter.Core.Domain.Delivery.Enums;
using Nurtricenter.Core.Domain.Delivery.Events;
using Nurtricenter.Core.Domain.Delivery.ValueObjects;

public sealed class Delivery : Entity
{
    public Guid RouteId { get; internal set; }
    public ValidatedPackage Package { get; private set; }
    public DeliveryAddress Address { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public int? SequenceOrder { get; internal set; }
    public DeliveryConfirmation? Confirmation { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; set; }

    public bool IsTerminal => Status == DeliveryStatus.Delivered || Status == DeliveryStatus.Failed;

    internal Delivery(Guid id, ValidatedPackage package, DeliveryAddress address)
        : base(id)
    {
        if (package is null)
            throw new DomainException(
                new Error(
                    "Delivery.NullPackage",
                    "Package is required.",
                    ErrorType.Validation));

        if (address is null)
            throw new DomainException(
                new Error(
                    "Delivery.NullAddress",
                    "Address is required.",
                    ErrorType.Validation));

        Package = package;
        Address = address;
        Status = DeliveryStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    private Delivery() : base() { }

    internal void StartDelivery()
    {
        if (Status != DeliveryStatus.Pending)
            throw new DomainException(
                Error.Problem(
                    "Delivery.AlreadyStarted",
                    "Cannot start a delivery that is {status}.",
                    Status.ToString()));

        Status = DeliveryStatus.InProgress;
    }

    internal void RegisterSuccessfulDelivery(DeliveryConfirmation confirmation)
    {
        if (confirmation is null)
            throw new DomainException(
                new Error(
                    "Delivery.NullConfirmation",
                    "Delivery confirmation is required.",
                    ErrorType.Validation));

        if (Status != DeliveryStatus.InProgress && Status != DeliveryStatus.Pending)
            throw new DomainException(
                Error.Problem(
                    "Delivery.CannotComplete",
                    "Cannot register a successful delivery when the delivery is {status}.",
                    Status.ToString()));

        Confirmation = confirmation;
        Status = DeliveryStatus.Delivered;

        AddDomainEvent(new DeliveryCompletedEvent(
            Id, RouteId, confirmation.DeliveredAt, confirmation.EvidencePhotoUrl, confirmation.DigitalSignature));
    }

    internal void RegisterFailedDelivery(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException(
                new Error(
                    "Delivery.EmptyFailureReason",
                    "Failure reason cannot be empty.",
                    ErrorType.Validation));

        if (Status != DeliveryStatus.InProgress && Status != DeliveryStatus.Pending)
            throw new DomainException(
                Error.Problem(
                    "Delivery.CannotFail",
                    "Cannot register a failed delivery when the delivery is {status}.",
                    Status.ToString()));

        FailureReason = reason;
        Status = DeliveryStatus.Failed;

        AddDomainEvent(new DeliveryFailedEvent(Id, RouteId, reason));
    }
}
