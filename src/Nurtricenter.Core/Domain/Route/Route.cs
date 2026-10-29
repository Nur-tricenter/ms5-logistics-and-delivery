namespace Nurtricenter.Core.Domain.Route;

using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using Nurtricenter.Core.Domain.Delivery.ValueObjects;
using Nurtricenter.Core.Domain.Route.Enums;
using Nurtricenter.Core.Domain.Route.Events;
using DeliveryEntity = Nurtricenter.Core.Domain.Delivery.Delivery;

public sealed class Route : AggregateRoot
{
    private readonly List<DeliveryEntity> _deliveries = new();

    public Guid CourierId { get; private set; }
    public DateOnly ScheduledDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public RouteStatus Status { get; private set; }
    public IReadOnlyList<DeliveryEntity> Deliveries => _deliveries.AsReadOnly();

    public Route(Guid id, Guid courierId, DateOnly scheduledDate)
        : base(id)
    {
        CourierId = courierId;
        ScheduledDate = scheduledDate;
        Status = RouteStatus.Pending;
        CreatedAt = DateTime.UtcNow;

        AddDomainEvent(new RouteCreatedEvent(id, courierId, scheduledDate));
    }

    private Route() : base() { }

    public void AssignCourier(Guid courierId)
    {
        if (Status == RouteStatus.Completed || Status == RouteStatus.Cancelled || Status == RouteStatus.InProgress)
            throw new DomainException(
                Error.Problem(
                    "Route.CannotAssignCourier",
                    "Cannot assign a courier to a {status} route.",
                    Status.ToString()));

        CourierId = courierId;

        AddDomainEvent(new CourierAssignedToRouteEvent(Id, courierId));
    }

    public void AddDelivery(ValidatedPackage package, DeliveryAddress address)
    {
        if (package is null)
            throw new DomainException(
                new Error(
                    "Route.NullPackage",
                    "Package is required.",
                    ErrorType.Validation));

        if (address is null)
            throw new DomainException(
                new Error(
                    "Route.NullAddress",
                    "Address is required.",
                    ErrorType.Validation));

        if (Status == RouteStatus.Completed || Status == RouteStatus.Cancelled)
            throw new DomainException(
                Error.Problem(
                    "Route.CannotAddDelivery",
                    "Cannot add deliveries to a {status} route.",
                    Status.ToString()));

        var delivery = new DeliveryEntity(Guid.NewGuid(), package, address);
        delivery.RouteId = Id;

        _deliveries.Add(delivery);

        AddDomainEvent(new DeliveryAddedToRouteEvent(Id, delivery.Id, package.PackageId, package.PatientId, package.DeliveryDayId));
    }

    public void StartRoute(Coordinate startingPoint)
    {
        if (Status != RouteStatus.Pending)
            throw new DomainException(
                Error.Problem(
                    "Route.CannotStart",
                    "Only pending routes can be started. Current status: {status}.",
                    Status.ToString()));

        if (_deliveries.Count == 0)
            throw new DomainException(
                Error.Problem(
                    "Route.NoDeliveries",
                    "Cannot start a route with no deliveries."));

        CalculateDeliverySequence(startingPoint);

        Status = RouteStatus.InProgress;

        foreach (var delivery in _deliveries)
        {
            delivery.StartDelivery();
        }

        AddDomainEvent(new RouteStartedEvent(Id));
    }

    private void CalculateDeliverySequence(Coordinate startingPoint)
    {
        var remaining = new List<DeliveryEntity>(_deliveries);
        var currentPoint = startingPoint;
        int sequence = 1;

        while (remaining.Count > 0)
        {
            var nearest = remaining
                .OrderBy(d => currentPoint.DistanceTo(d.Address.PlanarCoordinate))
                .First();

            nearest.SequenceOrder = sequence++;
            currentPoint = nearest.Address.PlanarCoordinate;
            remaining.Remove(nearest);
        }
    }

    public void CompleteRoute()
    {
        if (Status != RouteStatus.InProgress)
            throw new DomainException(
                Error.Problem(
                    "Route.CannotComplete",
                    "Only in-progress routes can be completed. Current status: {status}.",
                    Status.ToString()));

        if (_deliveries.Any(d => !d.IsTerminal))
            throw new DomainException(
                Error.Problem(
                    "Route.HasPendingDeliveries",
                    "Cannot complete a route with pending or in-progress deliveries."));

        Status = RouteStatus.Completed;

        AddDomainEvent(new RouteCompletedEvent(Id));
    }

    public void CancelRoute()
    {
        if (Status == RouteStatus.Completed || Status == RouteStatus.Cancelled)
            throw new DomainException(
                Error.Problem(
                    "Route.CannotCancel",
                    "A {status} route cannot be cancelled.",
                    Status.ToString()));

        foreach (var delivery in _deliveries.Where(d => !d.IsTerminal))
        {
            delivery.RegisterFailedDelivery("Route was cancelled.");
        }

        Status = RouteStatus.Cancelled;

        AddDomainEvent(new RouteCancelledEvent(Id));
    }

    public void CompleteDelivery(Guid deliveryId, DeliveryConfirmation confirmation)
    {
        if (confirmation is null)
            throw new DomainException(
                new Error(
                    "Route.NullConfirmation",
                    "Delivery confirmation is required.",
                    ErrorType.Validation));

        var delivery = FindDelivery(deliveryId);
        delivery.RegisterSuccessfulDelivery(confirmation);
    }

    public void FailDelivery(Guid deliveryId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException(
                new Error(
                    "Route.EmptyFailureReason",
                    "Failure reason cannot be empty.",
                    ErrorType.Validation));

        var delivery = FindDelivery(deliveryId);
        delivery.RegisterFailedDelivery(reason);
    }

    private DeliveryEntity FindDelivery(Guid deliveryId)
    {
        var delivery = _deliveries.FirstOrDefault(d => d.Id == deliveryId);

        if (delivery is null)
            throw new DomainException(
                Error.NotFound(
                    "Route.DeliveryNotFound",
                    "Delivery '{deliveryId}' not found in this route.",
                    deliveryId.ToString()));

        return delivery;
    }
}
