namespace Nurtricenter.Test.Routes.Application;

using FluentAssertions;
using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using Microsoft.Extensions.Options;
using Moq;
using Nurtricenter.Application.Commands.Routes.Start;
using Nurtricenter.Core.Domain.Route;
using Nurtricenter.Core.Domain.Route.Enums;
using Nurtricenter.Core.Domain.Route.Repositories;
using Nurtricenter.Core.Options;
using Nurtricenter.Test.Helpers;

public class StartRouteHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IOptions<BranchCoordinatesOptions> _branchOptions;
    private readonly StartRouteHandler _handler;

    public StartRouteHandlerTests()
    {
        _routeRepositoryMock = new Mock<IRouteRepository>();
        _unitOfWorkMock      = new Mock<IUnitOfWork>();
        _branchOptions       = Options.Create(new BranchCoordinatesOptions
        {
            Latitude  = RouteTestBuilder.DefaultLat,
            Longitude = RouteTestBuilder.DefaultLon
        });

        _handler = new StartRouteHandler(
            _routeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _branchOptions);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Happy-path – response shape
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidRoute_ReturnsRouteIdInResponse()
    {
        // Arrange
        var (command, route) = ArrangeValidScenario();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.RouteId.Should().Be(route.Id);
    }

    [Fact]
    public async Task Handle_WithValidRoute_ReturnsCourierIdInResponse()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var (command, _) = ArrangeValidScenario(courierId: courierId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.CourierId.Should().Be(courierId);
    }

    [Fact]
    public async Task Handle_WithOneDelivery_ReturnsOneDeliveryInResponse()
    {
        // Arrange
        var (command, _) = ArrangeValidScenario(deliveryCount: 1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Deliveries.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithMultipleDeliveries_ReturnsAllDeliveriesOrderedBySequence()
    {
        // Arrange
        var (command, _) = ArrangeValidScenario(deliveryCount: 3);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Deliveries.Should().HaveCount(3);
        result.Deliveries.Select(d => d.SequenceOrder)
            .Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Handle_WithDeliveries_EachDeliveryHasNonEmptyAddress()
    {
        // Arrange
        var (command, _) = ArrangeValidScenario(deliveryCount: 2);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Deliveries.Should().AllSatisfy(d => d.Address.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task Handle_WithDeliveries_EachDeliveryHasNonEmptyId()
    {
        // Arrange
        var (command, _) = ArrangeValidScenario(deliveryCount: 2);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Deliveries.Should().AllSatisfy(d => d.DeliveryId.Should().NotBeEmpty());
    }

    // ──────────────────────────────────────────────────────────────────────
    // Happy-path – domain state & side effects
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidRoute_ChangesRouteStatusToInProgress()
    {
        // Arrange
        var (command, route) = ArrangeValidScenario();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        route.Status.Should().Be(RouteStatus.InProgress);
    }

    [Fact]
    public async Task Handle_WithValidRoute_CommitsUnitOfWorkOnce()
    {
        // Arrange
        var (command, _) = ArrangeValidScenario();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(
            u => u.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidRoute_AssignsSequenceOrderToAllDeliveries()
    {
        // Arrange
        var (command, route) = ArrangeValidScenario(deliveryCount: 3);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        route.Deliveries.Should().AllSatisfy(d => d.SequenceOrder.Should().NotBeNull());
    }

    // ──────────────────────────────────────────────────────────────────────
    // Error path – route not found
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenRouteNotFound_ThrowsDomainExceptionWithNotFoundCode()
    {
        // Arrange
        var routeId = Guid.NewGuid();
        _routeRepositoryMock
            .Setup(r => r.GetByIdAsync(routeId, false))
            .ReturnsAsync((Route?)null);

        var command = new StartRouteCommand(routeId);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(ex => ex.Error.Code == "Route.NotFound");
    }

    [Fact]
    public async Task Handle_WhenRouteNotFound_DoesNotCommitUnitOfWork()
    {
        // Arrange
        var routeId = Guid.NewGuid();
        _routeRepositoryMock
            .Setup(r => r.GetByIdAsync(routeId, false))
            .ReturnsAsync((Route?)null);

        var command = new StartRouteCommand(routeId);

        // Act
        try { await _handler.Handle(command, CancellationToken.None); } catch { /* expected */ }

        // Assert
        _unitOfWorkMock.Verify(
            u => u.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Error path – domain guard on Route.StartRoute
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenRouteHasNoDeliveries_ThrowsDomainExceptionWithNoDeliveriesCode()
    {
        // Arrange
        var route = RouteTestBuilder.CreatePendingRoute();
        var command = new StartRouteCommand(route.Id);

        _routeRepositoryMock
            .Setup(r => r.GetByIdAsync(route.Id, false))
            .ReturnsAsync(route);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(ex => ex.Error.Code == "Route.NoDeliveries");
    }

    [Fact]
    public async Task Handle_WhenRouteAlreadyInProgress_ThrowsDomainExceptionWithCannotStartCode()
    {
        // Arrange
        var route = RouteTestBuilder.CreateInProgressRoute(deliveryCount: 1);
        var command = new StartRouteCommand(route.Id);

        _routeRepositoryMock
            .Setup(r => r.GetByIdAsync(route.Id, false))
            .ReturnsAsync(route);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(ex => ex.Error.Code == "Route.CannotStart");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Private helper
    // ──────────────────────────────────────────────────────────────────────

    private (StartRouteCommand command, Route route) ArrangeValidScenario(
        int deliveryCount = 2,
        Guid? courierId = null)
    {
        var route = RouteTestBuilder.CreatePendingRoute(courierId);
        RouteTestBuilder.AddDeliveries(route, deliveryCount);

        _routeRepositoryMock
            .Setup(r => r.GetByIdAsync(route.Id, false))
            .ReturnsAsync(route);

        return (new StartRouteCommand(route.Id), route);
    }
}
