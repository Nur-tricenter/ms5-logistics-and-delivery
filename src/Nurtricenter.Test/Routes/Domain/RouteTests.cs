namespace Nurtricenter.Test.Routes.Domain;

using FluentAssertions;
using Joseco.DDD.Core.Results;
using Nurtricenter.Core.Domain.Delivery.ValueObjects;
using Nurtricenter.Core.Domain.Route;
using Nurtricenter.Core.Domain.Route.Enums;
using Nurtricenter.Core.Domain.Route.Events;

public class RouteTests
{
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // Constructor
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void Constructor_WithValidArguments_SetsCourierIdCorrectly()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var route = new Route(Guid.NewGuid(), courierId, date);

        // Assert
        route.CourierId.Should().Be(courierId);
    }

    [Fact]
    public void Constructor_WithValidArguments_SetsScheduledDateCorrectly()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var route = new Route(Guid.NewGuid(), Guid.NewGuid(), date);

        // Assert
        route.ScheduledDate.Should().Be(date);
    }

    [Fact]
    public void Constructor_WithValidArguments_SetsStatusToPending()
    {
        // Act
        var route = new Route(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today));

        // Assert
        route.Status.Should().Be(RouteStatus.Pending);
    }

    [Fact]
    public void Constructor_WithValidArguments_StartsWithNoDeliveries()
    {
        // Act
        var route = new Route(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today));

        // Assert
        route.Deliveries.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithValidArguments_RaisesRouteCreatedEvent()
    {
        // Act
        var route = new Route(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today));

        // Assert
        route.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RouteCreatedEvent>();
    }

    [Fact]
    public void Constructor_WithValidArguments_RaisedEventContainsCorrectData()
    {
        // Arrange
        var routeId = Guid.NewGuid();
        var courierId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var route = new Route(routeId, courierId, date);

        // Assert
        var evt = route.DomainEvents.OfType<RouteCreatedEvent>().Single();
        evt.RouteId.Should().Be(routeId);
        evt.CourierId.Should().Be(courierId);
        evt.ScheduledDate.Should().Be(date);
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // AddDelivery
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void AddDelivery_WithValidPackageAndAddress_IncreasesDeliveryCount()
    {
        // Arrange
        var route = CreatePendingRoute();
        var (package, address) = CreateDeliveryComponents();

        // Act
        route.AddDelivery(package, address);

        // Assert
        route.Deliveries.Should().HaveCount(1);
    }

    [Fact]
    public void AddDelivery_WithMultipleDeliveries_AddsAllToRoute()
    {
        // Arrange
        var route = CreatePendingRoute();

        // Act
        route.AddDelivery(CreateDeliveryComponents().package, CreateDeliveryComponents("PKG-001").address);
        route.AddDelivery(CreateDeliveryComponents("PKG-002").package, CreateDeliveryComponents("PKG-002").address);

        // Assert
        route.Deliveries.Should().HaveCount(2);
    }

    [Fact]
    public void AddDelivery_WithNullPackage_ThrowsDomainException()
    {
        // Arrange
        var route = CreatePendingRoute();
        var (_, address) = CreateDeliveryComponents();

        // Act
        var act = () => route.AddDelivery(null!, address);

        // Assert
        act.Should().Throw<DomainException>()
            .Where(ex => ex.Error.Code == "Route.NullPackage");
    }

    [Fact]
    public void AddDelivery_WithNullAddress_ThrowsDomainException()
    {
        // Arrange
        var route = CreatePendingRoute();
        var (package, _) = CreateDeliveryComponents();

        // Act
        var act = () => route.AddDelivery(package, null!);

        // Assert
        act.Should().Throw<DomainException>()
            .Where(ex => ex.Error.Code == "Route.NullAddress");
    }

    [Fact]
    public void AddDelivery_ToCompletedRoute_ThrowsDomainException()
    {
        // Arrange
        var route = CreateInProgressRouteWithOneDelivery();
        CompleteAllDeliveries(route);
        route.CompleteRoute();

        var (package, address) = CreateDeliveryComponents("PKG-NEW");

        // Act
        var act = () => route.AddDelivery(package, address);

        // Assert
        act.Should().Throw<DomainException>()
            .Where(ex => ex.Error.Code == "Route.CannotAddDelivery");
    }

    [Fact]
    public void AddDelivery_ToCancelledRoute_ThrowsDomainException()
    {
        // Arrange
        var route = CreatePendingRoute();
        route.CancelRoute();

        var (package, address) = CreateDeliveryComponents();

        // Act
        var act = () => route.AddDelivery(package, address);

        // Assert
        act.Should().Throw<DomainException>()
            .Where(ex => ex.Error.Code == "Route.CannotAddDelivery");
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // Private helpers
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static Route CreatePendingRoute()
        => new(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today));

    private static (ValidatedPackage package, DeliveryAddress address) CreateDeliveryComponents(
        string suffix = "PKG-001")
        => (
            new ValidatedPackage(suffix, $"PAT-{suffix}", $"label-{suffix}"),
            new DeliveryAddress($"Street {suffix}", new Coordinate(1.0, 2.0))
        );

    private static Route CreateInProgressRouteWithOneDelivery()
    {
        var route = CreatePendingRoute();
        var (pkg, addr) = CreateDeliveryComponents();
        route.AddDelivery(pkg, addr);
        route.StartRoute(new Coordinate(0.0, 0.0));
        return route;
    }

    private static void CompleteAllDeliveries(Route route)
    {
        foreach (var delivery in route.Deliveries)
        {
            var confirmation = new DeliveryConfirmation(
                DateTime.UtcNow,
                "https://evidence.example.com/photo.jpg",
                "digital-signature-data");
            route.CompleteDelivery(delivery.Id, confirmation);
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // Group A — StartRoute domain tests
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void StartRoute_WithValidDeliveries_ChangesStatusToInProgress()
    {
        var route = CreatePendingRoute();
        var (pkg, addr) = CreateDeliveryComponents();
        route.AddDelivery(pkg, addr);

        route.StartRoute(new Coordinate(0.0, 0.0));

        route.Status.Should().Be(RouteStatus.InProgress);
    }

    [Fact]
    public void StartRoute_WithValidDeliveries_RaisesRouteStartedEvent()
    {
        var route = CreatePendingRoute();
        var (pkg, addr) = CreateDeliveryComponents();
        route.AddDelivery(pkg, addr);
        route.ClearDomainEvents();

        route.StartRoute(new Coordinate(0.0, 0.0));

        route.DomainEvents.OfType<RouteStartedEvent>().Should().ContainSingle();
    }

    [Fact]
    public void StartRoute_WithValidDeliveries_SetsSequenceOrderOnAllDeliveries()
    {
        var route = CreatePendingRoute();
        var (pkg, addr) = CreateDeliveryComponents();
        route.AddDelivery(pkg, addr);

        route.StartRoute(new Coordinate(0.0, 0.0));

        route.Deliveries.Should().AllSatisfy(d => d.SequenceOrder.Should().NotBeNull());
    }

    [Fact]
    public void StartRoute_WithValidDeliveries_AllDeliveriesBecomesInProgress()
    {
        var route = CreatePendingRoute();
        var (pkg, addr) = CreateDeliveryComponents();
        route.AddDelivery(pkg, addr);

        route.StartRoute(new Coordinate(0.0, 0.0));

        route.Deliveries.Should().AllSatisfy(d => d.Status.ToString().Should().Be("InProgress"));
    }

    [Fact]
    public void StartRoute_WhenNotPending_ThrowsDomainExceptionWithCannotStartCode()
    {
        var route = CreateInProgressRouteWithOneDelivery();

        var act = () => route.StartRoute(new Coordinate(0.0, 0.0));

        act.Should().Throw<DomainException>().Where(e => e.Error.Code == "Route.CannotStart");
    }

    [Fact]
    public void StartRoute_WithNoDeliveries_ThrowsDomainExceptionWithNoDeliveriesCode()
    {
        var route = CreatePendingRoute();

        var act = () => route.StartRoute(new Coordinate(0.0, 0.0));

        act.Should().Throw<DomainException>().Where(e => e.Error.Code == "Route.NoDeliveries");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Group B — CompleteRoute domain tests
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void CompleteRoute_WhenAllDeliveriesAreTerminal_ChangesStatusToCompleted()
    {
        var route = CreateInProgressRouteWithOneDelivery();
        CompleteAllDeliveries(route);

        route.CompleteRoute();

        route.Status.Should().Be(RouteStatus.Completed);
    }

    [Fact]
    public void CompleteRoute_WhenAllDeliveriesAreTerminal_RaisesRouteCompletedEvent()
    {
        var route = CreateInProgressRouteWithOneDelivery();
        CompleteAllDeliveries(route);
        route.ClearDomainEvents();

        route.CompleteRoute();

        route.DomainEvents.OfType<RouteCompletedEvent>().Should().ContainSingle();
    }

    [Fact]
    public void CompleteRoute_WhenNotInProgress_ThrowsDomainExceptionWithCannotCompleteCode()
    {
        var route = CreatePendingRoute();

        var act = () => route.CompleteRoute();

        act.Should().Throw<DomainException>().Where(e => e.Error.Code == "Route.CannotComplete");
    }

    [Fact]
    public void CompleteRoute_WhenHasNonTerminalDeliveries_ThrowsDomainExceptionWithHasPendingDeliveriesCode()
    {
        var route = CreatePendingRoute();
        var (pkg1, addr1) = CreateDeliveryComponents("1");
        var (pkg2, addr2) = CreateDeliveryComponents("2");
        route.AddDelivery(pkg1, addr1);
        route.AddDelivery(pkg2, addr2);
        route.StartRoute(new Coordinate(0.0, 0.0));
        
        var confirmation = new DeliveryConfirmation(DateTime.UtcNow, "url", "sig");
        route.CompleteDelivery(route.Deliveries[0].Id, confirmation);

        var act = () => route.CompleteRoute();

        act.Should().Throw<DomainException>().Where(e => e.Error.Code == "Route.HasPendingDeliveries");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Group C — CancelRoute domain tests
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void CancelRoute_WhenPending_ChangesStatusToCancelled()
    {
        var route = CreatePendingRoute();

        route.CancelRoute();

        route.Status.Should().Be(RouteStatus.Cancelled);
    }

    [Fact]
    public void CancelRoute_WhenPending_RaisesRouteCancelledEvent()
    {
        var route = CreatePendingRoute();
        route.ClearDomainEvents();

        route.CancelRoute();

        route.DomainEvents.OfType<RouteCancelledEvent>().Should().ContainSingle();
    }

    [Fact]
    public void CancelRoute_WhenInProgressWithActiveDeliveries_FailsAllNonTerminalDeliveries()
    {
        var route = CreatePendingRoute();
        var (pkg1, addr1) = CreateDeliveryComponents("1");
        var (pkg2, addr2) = CreateDeliveryComponents("2");
        route.AddDelivery(pkg1, addr1);
        route.AddDelivery(pkg2, addr2);
        route.StartRoute(new Coordinate(0.0, 0.0));
        
        var confirmation = new DeliveryConfirmation(DateTime.UtcNow, "url", "sig");
        route.CompleteDelivery(route.Deliveries[0].Id, confirmation);

        route.CancelRoute();

        route.Deliveries[0].Status.ToString().Should().Be("Delivered");
        route.Deliveries[1].Status.ToString().Should().Be("Failed");
    }

    [Fact]
    public void CancelRoute_WhenAlreadyCompleted_ThrowsDomainExceptionWithCannotCancelCode()
    {
        var route = CreateInProgressRouteWithOneDelivery();
        CompleteAllDeliveries(route);
        route.CompleteRoute();

        var act = () => route.CancelRoute();

        act.Should().Throw<DomainException>().Where(e => e.Error.Code == "Route.CannotCancel");
    }

    [Fact]
    public void CancelRoute_WhenAlreadyCancelled_ThrowsDomainExceptionWithCannotCancelCode()
    {
        var route = CreatePendingRoute();
        route.CancelRoute();

        var act = () => route.CancelRoute();

        act.Should().Throw<DomainException>().Where(e => e.Error.Code == "Route.CannotCancel");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Group D — CompleteDelivery / FailDelivery domain tests
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void CompleteDelivery_WithNullConfirmation_ThrowsDomainExceptionWithNullConfirmationCode()
    {
        var route = CreateInProgressRouteWithOneDelivery();
        
        var act = () => route.CompleteDelivery(route.Deliveries[0].Id, null!);

        act.Should().Throw<DomainException>().Where(e => e.Error.Code == "Route.NullConfirmation");
    }

    [Fact]
    public void CompleteDelivery_WithUnknownDeliveryId_ThrowsDomainExceptionWithDeliveryNotFoundCode()
    {
        var route = CreateInProgressRouteWithOneDelivery();
        var confirmation = new DeliveryConfirmation(DateTime.UtcNow, "url", "sig");
        
        var act = () => route.CompleteDelivery(Guid.NewGuid(), confirmation);

        act.Should().Throw<DomainException>().Where(e => e.Error.Code == "Route.DeliveryNotFound");
    }

    [Fact]
    public void FailDelivery_WithEmptyReason_ThrowsDomainExceptionWithEmptyReasonCode()
    {
        var route = CreateInProgressRouteWithOneDelivery();
        
        var act = () => route.FailDelivery(route.Deliveries[0].Id, "");

        act.Should().Throw<DomainException>().Where(e => e.Error.Code == "Route.EmptyFailureReason");
    }

    [Fact]
    public void FailDelivery_WithUnknownDeliveryId_ThrowsDomainExceptionWithDeliveryNotFoundCode()
    {
        var route = CreateInProgressRouteWithOneDelivery();
        
        var act = () => route.FailDelivery(Guid.NewGuid(), "reason");

        act.Should().Throw<DomainException>().Where(e => e.Error.Code == "Route.DeliveryNotFound");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Group E — AssignCourier domain tests
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void AssignCourier_WhenPending_UpdatesCourierId()
    {
        var route = CreatePendingRoute();
        var newCourierId = Guid.NewGuid();

        route.AssignCourier(newCourierId);

        route.CourierId.Should().Be(newCourierId);
    }

    [Fact]
    public void AssignCourier_WhenPending_RaisesCourierAssignedEvent()
    {
        var route = CreatePendingRoute();
        route.ClearDomainEvents();
        var newCourierId = Guid.NewGuid();

        route.AssignCourier(newCourierId);

        route.DomainEvents.OfType<Nurtricenter.Core.Domain.Route.Events.CourierAssignedToRouteEvent>().Should().ContainSingle();
    }

    [Fact]
    public void AssignCourier_WhenInProgress_ThrowsDomainExceptionWithCannotAssignCode()
    {
        var route = CreateInProgressRouteWithOneDelivery();

        var act = () => route.AssignCourier(Guid.NewGuid());

        act.Should().Throw<DomainException>().Where(e => e.Error.Code == "Route.CannotAssignCourier");
    }

    [Fact]
    public void AssignCourier_WhenCompleted_ThrowsDomainExceptionWithCannotAssignCode()
    {
        var route = CreateInProgressRouteWithOneDelivery();
        CompleteAllDeliveries(route);
        route.CompleteRoute();

        var act = () => route.AssignCourier(Guid.NewGuid());

        act.Should().Throw<DomainException>().Where(e => e.Error.Code == "Route.CannotAssignCourier");
    }

    [Fact]
    public void AssignCourier_WhenCancelled_ThrowsDomainExceptionWithCannotAssignCode()
    {
        var route = CreatePendingRoute();
        route.CancelRoute();

        var act = () => route.AssignCourier(Guid.NewGuid());

        act.Should().Throw<DomainException>().Where(e => e.Error.Code == "Route.CannotAssignCourier");
    }
}
