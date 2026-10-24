namespace Nurtricenter.Test.Deliveries.Application;

using FluentAssertions;
using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using Moq;
using Nurtricenter.Application.Commands.Deliveries.Complete;
using Nurtricenter.Core.Domain.Route;
using Nurtricenter.Core.Domain.Route.Repositories;
using Nurtricenter.Test.Helpers;

public class CompleteDeliveryHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CompleteDeliveryHandler _handler;

    public CompleteDeliveryHandlerTests()
    {
        _routeRepositoryMock = new Mock<IRouteRepository>();
        _unitOfWorkMock      = new Mock<IUnitOfWork>();

        _handler = new CompleteDeliveryHandler(
            _routeRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Happy-path – response shape
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsDeliveredStatus()
    {
        // Arrange
        var (command, _) = ArrangeValidScenario();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be("Delivered");
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsCorrectDeliveryId()
    {
        // Arrange
        var (command, _) = ArrangeValidScenario();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.DeliveryId.Should().Be(command.DeliveryId);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsCorrectRouteId()
    {
        // Arrange
        var (command, route) = ArrangeValidScenario();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.RouteId.Should().Be(route.Id);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsConfirmationWithMatchingEvidenceUrl()
    {
        // Arrange
        const string evidenceUrl = "https://evidence.example.com/photo.jpg";
        var (command, _) = ArrangeValidScenario(evidenceUrl: evidenceUrl);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Confirmation.EvidencePhotoUrl.Should().Be(evidenceUrl);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsConfirmationWithMatchingDigitalSignature()
    {
        // Arrange
        const string signature = "my-digital-signature";
        var (command, _) = ArrangeValidScenario(signature: signature);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Confirmation.DigitalSignature.Should().Be(signature);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Happy-path – side effects
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_CommitsUnitOfWorkOnce()
    {
        // Arrange
        var (command, _) = ArrangeValidScenario();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Error path – route not found
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenRouteNotFound_ThrowsDomainExceptionWithNotFoundCode()
    {
        // Arrange
        var routeId = Guid.NewGuid();
        _routeRepositoryMock.Setup(r => r.GetByIdAsync(routeId, false)).ReturnsAsync((Route?)null);

        var command = new CompleteDeliveryCommand(
            routeId, Guid.NewGuid(), DateTime.UtcNow,
            "https://evidence.example.com/photo.jpg", "sig");

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
        _routeRepositoryMock.Setup(r => r.GetByIdAsync(routeId, false)).ReturnsAsync((Route?)null);

        var command = new CompleteDeliveryCommand(
            routeId, Guid.NewGuid(), DateTime.UtcNow,
            "https://evidence.example.com/photo.jpg", "sig");

        // Act
        try { await _handler.Handle(command, CancellationToken.None); } catch { /* expected */ }

        // Assert
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Error path – delivery not found in route
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenDeliveryNotInRoute_ThrowsDomainExceptionWithDeliveryNotFoundCode()
    {
        // Arrange
        var route = RouteTestBuilder.CreateInProgressRoute(deliveryCount: 1);
        _routeRepositoryMock.Setup(r => r.GetByIdAsync(route.Id, false)).ReturnsAsync(route);

        var command = new CompleteDeliveryCommand(
            route.Id, Guid.NewGuid() /* wrong delivery id */, DateTime.UtcNow,
            "https://evidence.example.com/photo.jpg", "sig");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(ex => ex.Error.Code == "Route.DeliveryNotFound");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Private helper
    // ──────────────────────────────────────────────────────────────────────

    private (CompleteDeliveryCommand command, Route route) ArrangeValidScenario(
        string evidenceUrl = "https://evidence.example.com/photo.jpg",
        string signature = "valid-sig")
    {
        var route = RouteTestBuilder.CreateInProgressRoute(deliveryCount: 1);
        var delivery = route.Deliveries[0];

        _routeRepositoryMock
            .Setup(r => r.GetByIdAsync(route.Id, false))
            .ReturnsAsync(route);

        var command = new CompleteDeliveryCommand(
            route.Id, delivery.Id, DateTime.UtcNow, evidenceUrl, signature);

        return (command, route);
    }
}
