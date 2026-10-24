namespace Nurtricenter.Test.Deliveries.Application;

using FluentAssertions;
using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using Moq;
using Nurtricenter.Application.Commands.Deliveries.Add;
using Nurtricenter.Core.Domain.Route;
using Nurtricenter.Core.Domain.Route.Repositories;
using Nurtricenter.Core.Interfaces.Services.ClinicService;
using Nurtricenter.Core.Interfaces.Services.ClinicService.Dto;
using Nurtricenter.Test.Helpers;

public class AddDeliveriesHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepositoryMock;
    private readonly Mock<IClinicService> _clinicServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly AddDeliveriesHandler _handler;

    public AddDeliveriesHandlerTests()
    {
        _routeRepositoryMock = new Mock<IRouteRepository>();
        _clinicServiceMock   = new Mock<IClinicService>();
        _unitOfWorkMock      = new Mock<IUnitOfWork>();

        _handler = new AddDeliveriesHandler(
            _clinicServiceMock.Object,
            _routeRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Happy-path – response shape
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidItems_ReturnsRouteIdInResponse()
    {
        // Arrange
        var (command, route) = ArrangeValidScenario(itemCount: 1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.RouteId.Should().Be(route.Id);
    }

    [Fact]
    public async Task Handle_WithOneItem_ReturnsOneDeliveryInResponse()
    {
        // Arrange
        var (command, _) = ArrangeValidScenario(itemCount: 1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Deliveries.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithMultipleItems_ReturnsMatchingDeliveryCount()
    {
        // Arrange
        var (command, _) = ArrangeValidScenario(itemCount: 3);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Deliveries.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithValidItems_ReturnsDeliveriesWithPendingStatus()
    {
        // Arrange
        var (command, _) = ArrangeValidScenario(itemCount: 1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Deliveries.Should().AllSatisfy(d => d.Status.Should().Be("pending"));
    }

    [Fact]
    public async Task Handle_WithValidItems_ReturnsCorrectPatientIds()
    {
        // Arrange
        var items = new[]
        {
            MakeItem("PAT-001", "PKG-001"),
            MakeItem("PAT-002", "PKG-002")
        };
        var command = new AddDeliveriesCommand(items);
        var route = RouteTestBuilder.CreatePendingRoute();
        ArrangeClinicService(items.Select(i => i.PatientId).ToList());
        ArrangeRouteRepository(route);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Deliveries.Select(d => d.PatientId)
            .Should().BeEquivalentTo(new[] { "PAT-001", "PAT-002" });
    }

    // ──────────────────────────────────────────────────────────────────────
    // Happy-path – side effects
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidItems_CommitsUnitOfWorkOnce()
    {
        // Arrange
        var (command, _) = ArrangeValidScenario(itemCount: 1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithTwoItemsSamePatient_CallsClinicServiceWithOneDistinctId()
    {
        // Arrange
        const string sharedPatient = "PAT-SHARED";
        var items = new[]
        {
            MakeItem(sharedPatient, "PKG-A"),
            MakeItem(sharedPatient, "PKG-B")
        };
        var command = new AddDeliveriesCommand(items);
        var route = RouteTestBuilder.CreatePendingRoute();
        ArrangeClinicService(new[] { sharedPatient });
        ArrangeRouteRepository(route);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _clinicServiceMock.Verify(
            s => s.GetPatientsContactInfoAsync(
                It.Is<IReadOnlyList<string>>(ids => ids.Count == 1 && ids[0] == sharedPatient),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Error path – patients not found
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenPatientNotFound_ThrowsDomainExceptionWithPatientsNotFoundCode()
    {
        // Arrange
        var item = MakeItem("PAT-GHOST", "PKG-001");
        var command = new AddDeliveriesCommand(new[] { item });

        // Clinic service returns empty — patient not found
        _clinicServiceMock
            .Setup(s => s.GetPatientsContactInfoAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PatientContactInfo>());

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(ex => ex.Error.Code == "ClinicService.PatientsNotFound");
    }

    [Fact]
    public async Task Handle_WhenPatientNotFound_DoesNotCallRouteRepository()
    {
        // Arrange
        var item = MakeItem("PAT-GHOST", "PKG-001");
        var command = new AddDeliveriesCommand(new[] { item });

        _clinicServiceMock
            .Setup(s => s.GetPatientsContactInfoAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PatientContactInfo>());

        // Act
        try { await _handler.Handle(command, CancellationToken.None); } catch { /* expected */ }

        // Assert
        _routeRepositoryMock.Verify(
            r => r.GetLatestRouteForTodayAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Error path – no route for today
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoRouteForToday_ThrowsDomainExceptionWithNoRouteCode()
    {
        // Arrange
        var item = MakeItem("PAT-001", "PKG-001");
        var command = new AddDeliveriesCommand(new[] { item });

        _clinicServiceMock
            .Setup(s => s.GetPatientsContactInfoAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PatientContactInfo> { new("PAT-001", "555-0000", "Patient One") });

        _routeRepositoryMock
            .Setup(r => r.GetLatestRouteForTodayAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(ex => ex.Error.Code == "Route.NoRouteForToday");
    }

    [Fact]
    public async Task Handle_WhenNoRouteForToday_DoesNotCommitUnitOfWork()
    {
        // Arrange
        var item = MakeItem("PAT-001", "PKG-001");
        var command = new AddDeliveriesCommand(new[] { item });

        _clinicServiceMock
            .Setup(s => s.GetPatientsContactInfoAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PatientContactInfo> { new("PAT-001", "555-0000", "Patient One") });

        _routeRepositoryMock
            .Setup(r => r.GetLatestRouteForTodayAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        // Act
        try { await _handler.Handle(command, CancellationToken.None); } catch { /* expected */ }

        // Assert
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────

    private static AddDeliveryItem MakeItem(string patientId, string packageId)
        => new(patientId, packageId, $"Street {packageId}", 1.0, 2.0, "details");

    private void ArrangeClinicService(IEnumerable<string> patientIds)
    {
        var infos = patientIds
            .Select(id => new PatientContactInfo(id, "555-0000", $"Patient {id}"))
            .ToList();

        _clinicServiceMock
            .Setup(s => s.GetPatientsContactInfoAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(infos);
    }

    private void ArrangeRouteRepository(Route route)
    {
        _routeRepositoryMock
            .Setup(r => r.GetLatestRouteForTodayAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
    }

    private (AddDeliveriesCommand command, Route route) ArrangeValidScenario(int itemCount)
    {
        var route = RouteTestBuilder.CreatePendingRoute();

        var items = Enumerable.Range(1, itemCount)
            .Select(i => MakeItem($"PAT-{i:000}", $"PKG-{i:000}"))
            .ToList();

        ArrangeClinicService(items.Select(i => i.PatientId));
        ArrangeRouteRepository(route);

        return (new AddDeliveriesCommand(items), route);
    }
}
