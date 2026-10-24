namespace Nurtricenter.Test.Routes.Application;

using FluentAssertions;
using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using Moq;
using Nurtricenter.Application.Commands.Routes.Create;
using Nurtricenter.Core.Domain.Courier;
using Nurtricenter.Core.Domain.Courier.Repositories;
using Nurtricenter.Core.Domain.Route;
using Nurtricenter.Core.Domain.Route.Enums;
using Nurtricenter.Core.Domain.Route.Repositories;

public class CreateRouteHandlerTests
{
    private readonly Mock<ICourierRepository> _courierRepositoryMock;
    private readonly Mock<IRouteRepository> _routeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateRouteHandler _handler;

    public CreateRouteHandlerTests()
    {
        _courierRepositoryMock = new Mock<ICourierRepository>();
        _routeRepositoryMock   = new Mock<IRouteRepository>();
        _unitOfWorkMock        = new Mock<IUnitOfWork>();

        _handler = new CreateRouteHandler(
            _courierRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Happy-path – response shape
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsResponseWithNonEmptyId()
    {
        // Arrange
        var command = ArrangeValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsResponseWithMatchingCourierId()
    {
        // Arrange
        var command = ArrangeValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.CourierId.Should().Be(command.CourierId);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsResponseWithMatchingScheduledDate()
    {
        // Arrange
        var command = ArrangeValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ScheduledDate.Should().Be(command.ScheduledDate);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsResponseWithPendingStatus()
    {
        // Arrange
        var command = ArrangeValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(RouteStatus.Pending);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Happy-path – side-effect verification
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_AddsRouteToRepositoryOnce()
    {
        // Arrange
        var command = ArrangeValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _routeRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Route>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CommitsUnitOfWorkOnce()
    {
        // Arrange
        var command = ArrangeValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(
            u => u.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_AddsRouteWithCorrectCourierId()
    {
        // Arrange
        var command = ArrangeValidCommand();
        Route? capturedRoute = null;

        _routeRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Route>()))
            .Callback<Route>(r => capturedRoute = r);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedRoute!.CourierId.Should().Be(command.CourierId);
    }

    [Fact]
    public async Task Handle_WithValidCommand_AddsRouteWithCorrectScheduledDate()
    {
        // Arrange
        var command = ArrangeValidCommand();
        Route? capturedRoute = null;

        _routeRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Route>()))
            .Callback<Route>(r => capturedRoute = r);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedRoute!.ScheduledDate.Should().Be(command.ScheduledDate);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Error path – courier not found
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCourierNotFound_ThrowsDomainExceptionWithNotFoundCode()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var command = new CreateRouteCommand(courierId, DateOnly.FromDateTime(DateTime.Today));

        _courierRepositoryMock
            .Setup(r => r.GetByIdAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Courier?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(ex => ex.Error.Code == "Courier.NotFound");
    }

    [Fact]
    public async Task Handle_WhenCourierNotFound_DoesNotAddAnyRouteToRepository()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var command = new CreateRouteCommand(courierId, DateOnly.FromDateTime(DateTime.Today));

        _courierRepositoryMock
            .Setup(r => r.GetByIdAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Courier?)null);

        // Act
        try { await _handler.Handle(command, CancellationToken.None); } catch { /* expected */ }

        // Assert
        _routeRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Route>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCourierNotFound_DoesNotCommitUnitOfWork()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var command = new CreateRouteCommand(courierId, DateOnly.FromDateTime(DateTime.Today));

        _courierRepositoryMock
            .Setup(r => r.GetByIdAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Courier?)null);

        // Act
        try { await _handler.Handle(command, CancellationToken.None); } catch { /* expected */ }

        // Assert
        _unitOfWorkMock.Verify(
            u => u.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Private helper
    // ──────────────────────────────────────────────────────────────────────

    private CreateRouteCommand ArrangeValidCommand(DateOnly? scheduledDate = null)
    {
        var courierId = Guid.NewGuid();
        var date = scheduledDate ?? DateOnly.FromDateTime(DateTime.Today);

        _courierRepositoryMock
            .Setup(r => r.GetByIdAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Courier(courierId, "Test Courier"));

        return new CreateRouteCommand(courierId, date);
    }
}
