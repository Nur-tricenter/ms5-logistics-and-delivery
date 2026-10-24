namespace Nurtricenter.Test.Couriers.Application;

using FluentAssertions;
using Joseco.DDD.Core.Results;
using Moq;
using Nurtricenter.Application.Queries.Couriers.GetTodayRoute;
using Nurtricenter.Core.Domain.Courier;
using Nurtricenter.Core.Domain.Courier.Repositories;
using Nurtricenter.Core.Domain.Delivery.ValueObjects;
using Nurtricenter.Core.Domain.Route;
using Nurtricenter.Core.Domain.Route.Repositories;
using Nurtricenter.Core.Interfaces.Services.ClinicService;
using Nurtricenter.Core.Interfaces.Services.ClinicService.Dto;

public class GetCourierTodayRouteHandlerTests
{
    private readonly Mock<ICourierRepository> _courierRepositoryMock;
    private readonly Mock<IRouteRepository> _routeRepositoryMock;
    private readonly Mock<IClinicService> _clinicServiceMock;
    private readonly GetCourierTodayRouteHandler _handler;

    public GetCourierTodayRouteHandlerTests()
    {
        _courierRepositoryMock = new Mock<ICourierRepository>();
        _routeRepositoryMock = new Mock<IRouteRepository>();
        _clinicServiceMock = new Mock<IClinicService>();

        _handler = new GetCourierTodayRouteHandler(
            _courierRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _clinicServiceMock.Object);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Happy-path tests
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCourierAndRoute_ReturnsCourierIdInResponse()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var (query, _) = ArrangeValidScenario(courierId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.CourierId.Should().Be(courierId);
    }

    [Fact]
    public async Task Handle_WithValidCourierAndRoute_ReturnsRouteIdInResponse()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var (query, route) = ArrangeValidScenario(courierId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.RouteId.Should().Be(route.Id);
    }

    [Fact]
    public async Task Handle_WithOneDelivery_ReturnsOneDeliveryInResponse()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var (query, _) = ArrangeValidScenario(courierId, deliveryCount: 1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Deliveries.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithMultipleDeliveries_ReturnsAllDeliveries()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var (query, _) = ArrangeValidScenario(courierId, deliveryCount: 3);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Deliveries.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WhenPatientNameIsFoundInClinicService_MapsPatientNameCorrectly()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        const string patientId = "PAT-001";
        const string expectedName = "John Doe";

        var courier = new Courier(courierId, "Courier One");
        var today = DateOnly.FromDateTime(DateTime.Today);
        var route = new Route(Guid.NewGuid(), courierId, today);

        route.AddDelivery(
            new ValidatedPackage("PKG-001", patientId, "label-data"),
            new DeliveryAddress("123 Main St", new Coordinate(1.0, 2.0)));

        _courierRepositoryMock
            .Setup(r => r.GetByIdAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courier);

        _routeRepositoryMock
            .Setup(r => r.GetByCourierAndDateAsync(courierId, today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        _clinicServiceMock
            .Setup(s => s.GetPatientsContactInfoAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PatientContactInfo> { new(patientId, "555-0000", expectedName) });

        var query = new GetCourierTodayRouteQuery(courierId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Deliveries[0].PatientName.Should().Be(expectedName);
    }

    [Fact]
    public async Task Handle_WhenPatientNotFoundInClinicService_FallsBackToPatientId()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        const string patientId = "PAT-UNKNOWN";

        var courier = new Courier(courierId, "Courier One");
        var today = DateOnly.FromDateTime(DateTime.Today);
        var route = new Route(Guid.NewGuid(), courierId, today);

        route.AddDelivery(
            new ValidatedPackage("PKG-002", patientId, "label-data"),
            new DeliveryAddress("456 Oak Ave", new Coordinate(3.0, 4.0)));

        _courierRepositoryMock
            .Setup(r => r.GetByIdAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courier);

        _routeRepositoryMock
            .Setup(r => r.GetByCourierAndDateAsync(courierId, today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        _clinicServiceMock
            .Setup(s => s.GetPatientsContactInfoAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PatientContactInfo>());

        var query = new GetCourierTodayRouteQuery(courierId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Deliveries[0].PatientName.Should().Be(patientId);
    }

    [Fact]
    public async Task Handle_WithDelivery_ReturnsCorrectAddressDescription()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        const string expectedAddress = "789 Elm Street";

        var courier = new Courier(courierId, "Courier Two");
        var today = DateOnly.FromDateTime(DateTime.Today);
        var route = new Route(Guid.NewGuid(), courierId, today);

        route.AddDelivery(
            new ValidatedPackage("PKG-003", "PAT-002", "label"),
            new DeliveryAddress(expectedAddress, new Coordinate(5.0, 6.0)));

        _courierRepositoryMock
            .Setup(r => r.GetByIdAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courier);

        _routeRepositoryMock
            .Setup(r => r.GetByCourierAndDateAsync(courierId, today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        _clinicServiceMock
            .Setup(s => s.GetPatientsContactInfoAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PatientContactInfo>());

        var query = new GetCourierTodayRouteQuery(courierId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Deliveries[0].Address.Should().Be(expectedAddress);
    }

    [Fact]
    public async Task Handle_WithPendingDelivery_ReturnsDeliveryStatusAsPending()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var (query, _) = ArrangeValidScenario(courierId, deliveryCount: 1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Deliveries[0].Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Handle_WithDeliveryWithoutSequenceOrder_AssignsFallbackSequenceOrderStartingAtOne()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var (query, _) = ArrangeValidScenario(courierId, deliveryCount: 1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        // SequenceOrder is null on new deliveries; handler assigns index + 1 (=> 1 for the first)
        result.Deliveries[0].SequenceOrder.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithValidQuery_CallsCourierRepositoryExactlyOnceWithCorrectId()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var (query, _) = ArrangeValidScenario(courierId);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _courierRepositoryMock.Verify(
            r => r.GetByIdAsync(courierId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidQuery_CallsRouteRepositoryWithCorrectCourierIdAndToday()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var (query, _) = ArrangeValidScenario(courierId);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _routeRepositoryMock.Verify(
            r => r.GetByCourierAndDateAsync(courierId, today, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithTwoDeliveriesSharingPatientId_CallsClinicServiceWithOneDistinctPatientId()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        const string sharedPatientId = "PAT-SHARED";

        var courier = new Courier(courierId, "Courier Three");
        var today = DateOnly.FromDateTime(DateTime.Today);
        var route = new Route(Guid.NewGuid(), courierId, today);

        route.AddDelivery(
            new ValidatedPackage("PKG-A", sharedPatientId, "lbl"),
            new DeliveryAddress("Addr A", new Coordinate(1.0, 1.0)));

        route.AddDelivery(
            new ValidatedPackage("PKG-B", sharedPatientId, "lbl"),
            new DeliveryAddress("Addr B", new Coordinate(2.0, 2.0)));

        _courierRepositoryMock
            .Setup(r => r.GetByIdAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courier);

        _routeRepositoryMock
            .Setup(r => r.GetByCourierAndDateAsync(courierId, today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        _clinicServiceMock
            .Setup(s => s.GetPatientsContactInfoAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PatientContactInfo>());

        var query = new GetCourierTodayRouteQuery(courierId);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _clinicServiceMock.Verify(
            s => s.GetPatientsContactInfoAsync(
                It.Is<IReadOnlyList<string>>(ids => ids.Count == 1 && ids[0] == sharedPatientId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Error / not-found tests
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCourierNotFound_ThrowsDomainExceptionWithNotFoundCode()
    {
        // Arrange
        var courierId = Guid.NewGuid();

        _courierRepositoryMock
            .Setup(r => r.GetByIdAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Courier?)null);

        var query = new GetCourierTodayRouteQuery(courierId);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(ex => ex.Error.Code == "Courier.NotFound");
    }

    [Fact]
    public async Task Handle_WhenCourierNotFound_DoesNotCallRouteRepository()
    {
        // Arrange
        var courierId = Guid.NewGuid();

        _courierRepositoryMock
            .Setup(r => r.GetByIdAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Courier?)null);

        var query = new GetCourierTodayRouteQuery(courierId);

        // Act
        try { await _handler.Handle(query, CancellationToken.None); } catch { /* expected */ }

        // Assert
        _routeRepositoryMock.Verify(
            r => r.GetByCourierAndDateAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoRouteForToday_ThrowsDomainExceptionWithNoRouteCode()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var courier = new Courier(courierId, "Courier Four");

        _courierRepositoryMock
            .Setup(r => r.GetByIdAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courier);

        _routeRepositoryMock
            .Setup(r => r.GetByCourierAndDateAsync(courierId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        var query = new GetCourierTodayRouteQuery(courierId);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(ex => ex.Error.Code == "Route.NoRouteForToday");
    }

    [Fact]
    public async Task Handle_WhenNoRouteForToday_DoesNotCallClinicService()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var courier = new Courier(courierId, "Courier Five");

        _courierRepositoryMock
            .Setup(r => r.GetByIdAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courier);

        _routeRepositoryMock
            .Setup(r => r.GetByCourierAndDateAsync(courierId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        var query = new GetCourierTodayRouteQuery(courierId);

        // Act
        try { await _handler.Handle(query, CancellationToken.None); } catch { /* expected */ }

        // Assert
        _clinicServiceMock.Verify(
            s => s.GetPatientsContactInfoAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets up the three mocks for a valid happy-path scenario and returns the
    /// query and the route created so individual tests can make assertions on them.
    /// </summary>
    private (GetCourierTodayRouteQuery query, Route route) ArrangeValidScenario(
        Guid courierId,
        int deliveryCount = 2)
    {
        var courier = new Courier(courierId, "Test Courier");
        var today = DateOnly.FromDateTime(DateTime.Today);
        var route = new Route(Guid.NewGuid(), courierId, today);

        for (int i = 1; i <= deliveryCount; i++)
        {
            var package = new ValidatedPackage($"PKG-{i:000}", $"PAT-{i:000}", $"label-{i}");
            var address = new DeliveryAddress($"Street {i}", new Coordinate(i * 1.0, i * 2.0));
            route.AddDelivery(package, address);
        }

        _courierRepositoryMock
            .Setup(r => r.GetByIdAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courier);

        _routeRepositoryMock
            .Setup(r => r.GetByCourierAndDateAsync(courierId, today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var patients = Enumerable.Range(1, deliveryCount)
            .Select(i => new PatientContactInfo($"PAT-{i:000}", $"555-{i:0000}", $"Patient {i}"))
            .ToList();

        _clinicServiceMock
            .Setup(s => s.GetPatientsContactInfoAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(patients);

        return (new GetCourierTodayRouteQuery(courierId), route);
    }
}
