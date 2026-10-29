namespace Nurtricenter.Test.Deliveries.Application;

using FluentAssertions;
using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using Moq;
using Nurtricenter.Application.Commands.Deliveries.Add;
using Nurtricenter.Core.Domain.Route;
using Nurtricenter.Core.Domain.Route.Repositories;
using Nurtricenter.Test.Helpers;

public class AddDeliveriesHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly AddDeliveriesHandler _handler;

    public AddDeliveriesHandlerTests()
    {
        _routeRepositoryMock = new Mock<IRouteRepository>();
        _unitOfWorkMock      = new Mock<IUnitOfWork>();

        _handler = new AddDeliveriesHandler(
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
        ArrangeRouteRepository(route);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Deliveries.Select(d => d.PatientId)
            .Should().BeEquivalentTo(new[] { "PAT-001", "PAT-002" });
    }

    [Fact]
    public async Task Handle_WithValidItems_StoresPatientNameOnThePackage()
    {
        // Arrange
        var (command, route) = ArrangeValidScenario(itemCount: 1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        route.Deliveries[^1].Package.PatientName.Should().Be("Patient PAT-001");
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
    public async Task Handle_WithValidItems_LooksUpTodaysRoute()
    {
        // Arrange
        var (command, _) = ArrangeValidScenario(itemCount: 1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _routeRepositoryMock.Verify(
            r => r.GetLatestRouteForDateAsync(DateOnly.FromDateTime(DateTime.Today), It.IsAny<CancellationToken>()),
            Times.Once);
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

        _routeRepositoryMock
            .Setup(r => r.GetLatestRouteForDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
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

        _routeRepositoryMock
            .Setup(r => r.GetLatestRouteForDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
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
        => new(patientId, $"Patient {patientId}", packageId, $"Street {packageId}", 1.0, 2.0, "details");

    private void ArrangeRouteRepository(Route route)
    {
        _routeRepositoryMock
            .Setup(r => r.GetLatestRouteForDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
    }

    private (AddDeliveriesCommand command, Route route) ArrangeValidScenario(int itemCount)
    {
        var route = RouteTestBuilder.CreatePendingRoute();

        var items = Enumerable.Range(1, itemCount)
            .Select(i => MakeItem($"PAT-{i:000}", $"PKG-{i:000}"))
            .ToList();

        ArrangeRouteRepository(route);

        return (new AddDeliveriesCommand(items), route);
    }
}
