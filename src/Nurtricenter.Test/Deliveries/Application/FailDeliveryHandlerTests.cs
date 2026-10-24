namespace Nurtricenter.Test.Deliveries.Application;

using FluentAssertions;
using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using Moq;
using Nurtricenter.Application.Commands.Deliveries.Fail;
using Nurtricenter.Core.Domain.Route;
using Nurtricenter.Core.Domain.Route.Repositories;
using Nurtricenter.Test.Helpers;

public class FailDeliveryHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly FailDeliveryHandler _handler;

    public FailDeliveryHandlerTests()
    {
        _routeRepositoryMock = new Mock<IRouteRepository>();
        _unitOfWorkMock      = new Mock<IUnitOfWork>();

        _handler = new FailDeliveryHandler(
            _routeRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Happy-path – response shape
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsFailedStatus()
    {
        var (command, _) = ArrangeValidScenario();
        var result = await _handler.Handle(command, CancellationToken.None);
        result.Status.Should().Be("Failed");
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsCorrectDeliveryId()
    {
        var (command, _) = ArrangeValidScenario();
        var result = await _handler.Handle(command, CancellationToken.None);
        result.DeliveryId.Should().Be(command.DeliveryId);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsCorrectRouteId()
    {
        var (command, route) = ArrangeValidScenario();
        var result = await _handler.Handle(command, CancellationToken.None);
        result.RouteId.Should().Be(route.Id);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsMatchingFailureReason()
    {
        // Arrange
        const string reason = "Package was damaged.";
        var (command, _) = ArrangeValidScenario(reason: reason);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.FailureReason.Should().Be(reason);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Happy-path – side effects
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_CommitsUnitOfWorkOnce()
    {
        var (command, _) = ArrangeValidScenario();
        await _handler.Handle(command, CancellationToken.None);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Error path – route not found
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenRouteNotFound_ThrowsDomainExceptionWithNotFoundCode()
    {
        var routeId = Guid.NewGuid();
        _routeRepositoryMock.Setup(r => r.GetByIdAsync(routeId, false)).ReturnsAsync((Route?)null);

        var command = new FailDeliveryCommand(routeId, Guid.NewGuid(), "some reason");
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .Where(ex => ex.Error.Code == "Route.NotFound");
    }

    [Fact]
    public async Task Handle_WhenRouteNotFound_DoesNotCommitUnitOfWork()
    {
        var routeId = Guid.NewGuid();
        _routeRepositoryMock.Setup(r => r.GetByIdAsync(routeId, false)).ReturnsAsync((Route?)null);

        var command = new FailDeliveryCommand(routeId, Guid.NewGuid(), "some reason");
        try { await _handler.Handle(command, CancellationToken.None); } catch { /* expected */ }

        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Error path – delivery not found in route
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenDeliveryNotInRoute_ThrowsDomainExceptionWithDeliveryNotFoundCode()
    {
        var route = RouteTestBuilder.CreateInProgressRoute(deliveryCount: 1);
        _routeRepositoryMock.Setup(r => r.GetByIdAsync(route.Id, false)).ReturnsAsync(route);

        var command = new FailDeliveryCommand(route.Id, Guid.NewGuid() /* wrong id */, "reason");
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .Where(ex => ex.Error.Code == "Route.DeliveryNotFound");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Error path – empty failure reason (domain guard)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenFailureReasonIsEmpty_ThrowsDomainExceptionWithEmptyReasonCode()
    {
        var route = RouteTestBuilder.CreateInProgressRoute(deliveryCount: 1);
        _routeRepositoryMock.Setup(r => r.GetByIdAsync(route.Id, false)).ReturnsAsync(route);

        var command = new FailDeliveryCommand(route.Id, route.Deliveries[0].Id, "");
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .Where(ex => ex.Error.Code == "Route.EmptyFailureReason");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Private helper
    // ──────────────────────────────────────────────────────────────────────

    private (FailDeliveryCommand command, Route route) ArrangeValidScenario(
        string reason = "Customer was not home.")
    {
        var route = RouteTestBuilder.CreateInProgressRoute(deliveryCount: 1);
        var delivery = route.Deliveries[0];

        _routeRepositoryMock
            .Setup(r => r.GetByIdAsync(route.Id, false))
            .ReturnsAsync(route);

        return (new FailDeliveryCommand(route.Id, delivery.Id, reason), route);
    }
}
