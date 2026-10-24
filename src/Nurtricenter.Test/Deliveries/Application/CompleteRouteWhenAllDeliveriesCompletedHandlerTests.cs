namespace Nurtricenter.Test.Deliveries.Application;

using FluentAssertions;
using Joseco.DDD.Core.Abstractions;
using Moq;
using Nurtricenter.Application.Deliveries.Events;
using Nurtricenter.Core.Domain.Delivery.Events;
using Nurtricenter.Core.Domain.Route;
using Nurtricenter.Core.Domain.Route.Enums;
using Nurtricenter.Core.Domain.Route.Repositories;
using Nurtricenter.Test.Helpers;

public class CompleteRouteWhenAllDeliveriesCompletedHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CompleteRouteWhenAllDeliveriesCompletedHandler _handler;

    public CompleteRouteWhenAllDeliveriesCompletedHandlerTests()
    {
        _routeRepositoryMock = new Mock<IRouteRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CompleteRouteWhenAllDeliveriesCompletedHandler(
            _routeRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_DeliveryCompleted_WhenRouteNotFound_DoesNotCommit()
    {
        var routeId = Guid.NewGuid();
        _routeRepositoryMock.Setup(r => r.GetByIdAsync(routeId, false)).ReturnsAsync((Route?)null);

        await _handler.Handle(new DeliveryCompletedEvent(Guid.NewGuid(), routeId, DateTime.UtcNow), CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeliveryCompleted_WhenRouteAlreadyCompleted_DoesNotCommit()
    {
        var route = RouteTestBuilder.CreateInProgressRoute(1);
        route.CompleteDelivery(route.Deliveries[0].Id, RouteTestBuilder.MakeConfirmation());
        route.CompleteRoute();

        _routeRepositoryMock.Setup(r => r.GetByIdAsync(route.Id, false)).ReturnsAsync(route);

        await _handler.Handle(new DeliveryCompletedEvent(route.Deliveries[0].Id, route.Id, DateTime.UtcNow), CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeliveryCompleted_WhenRouteAlreadyCancelled_DoesNotCommit()
    {
        var route = RouteTestBuilder.CreatePendingRoute();
        route.CancelRoute();

        _routeRepositoryMock.Setup(r => r.GetByIdAsync(route.Id, false)).ReturnsAsync(route);

        await _handler.Handle(new DeliveryCompletedEvent(Guid.NewGuid(), route.Id, DateTime.UtcNow), CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeliveryCompleted_WhenNotAllDeliveriesTerminal_DoesNotCommit()
    {
        var route = RouteTestBuilder.CreateInProgressRoute(2);
        route.CompleteDelivery(route.Deliveries[0].Id, RouteTestBuilder.MakeConfirmation());

        _routeRepositoryMock.Setup(r => r.GetByIdAsync(route.Id, false)).ReturnsAsync(route);

        await _handler.Handle(new DeliveryCompletedEvent(route.Deliveries[0].Id, route.Id, DateTime.UtcNow), CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        route.Status.Should().Be(RouteStatus.InProgress);
    }

    [Fact]
    public async Task Handle_DeliveryCompleted_WhenAllDeliveriesTerminal_CompletesRouteAndCommits()
    {
        var route = RouteTestBuilder.CreateInProgressRoute(2);
        route.CompleteDelivery(route.Deliveries[0].Id, RouteTestBuilder.MakeConfirmation());
        route.CompleteDelivery(route.Deliveries[1].Id, RouteTestBuilder.MakeConfirmation());

        _routeRepositoryMock.Setup(r => r.GetByIdAsync(route.Id, false)).ReturnsAsync(route);

        await _handler.Handle(new DeliveryCompletedEvent(route.Deliveries[1].Id, route.Id, DateTime.UtcNow), CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        route.Status.Should().Be(RouteStatus.Completed);
    }

    [Fact]
    public async Task Handle_DeliveryFailed_WhenAllDeliveriesTerminal_CompletesRouteAndCommits()
    {
        var route = RouteTestBuilder.CreateInProgressRoute(1);
        route.FailDelivery(route.Deliveries[0].Id, "Customer not home");

        _routeRepositoryMock.Setup(r => r.GetByIdAsync(route.Id, false)).ReturnsAsync(route);

        await _handler.Handle(new DeliveryFailedEvent(route.Deliveries[0].Id, route.Id, "Customer not home"), CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        route.Status.Should().Be(RouteStatus.Completed);
    }
}
