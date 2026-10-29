namespace Nurtricenter.Test.Deliveries.Application;

using FluentAssertions;
using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nurtricenter.Application.Commands.Deliveries.AddValidatedPackage;
using Nurtricenter.Core.Domain.Delivery.Enums;
using Nurtricenter.Core.Domain.Route;
using Nurtricenter.Core.Domain.Route.Repositories;
using Nurtricenter.Core.Options;
using Nurtricenter.Test.Helpers;

public class AddValidatedPackageHandlerTests
{
    private const double BranchLatitude = -17.77;
    private const double BranchLongitude = -63.18;

    private readonly Mock<IRouteRepository> _routeRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly AddValidatedPackageHandler _handler;

    public AddValidatedPackageHandlerTests()
    {
        var branch = Microsoft.Extensions.Options.Options.Create(new BranchCoordinatesOptions
        {
            Latitude = BranchLatitude,
            Longitude = BranchLongitude
        });

        _handler = new AddValidatedPackageHandler(
            _routeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            branch,
            NullLogger<AddValidatedPackageHandler>.Instance);
    }

    private static AddValidatedPackageCommand MakeCommand(double? latitude = -17.80, double? longitude = -63.20)
        => new("PKG-001", "PAT-001", "Av. Banzer 123", latitude, longitude, "NTR-001");

    private Route ArrangeRouteForToday()
    {
        var route = RouteTestBuilder.CreatePendingRoute();
        _routeRepositoryMock
            .Setup(r => r.GetLatestRouteForTodayAsync(DateOnly.FromDateTime(DateTime.Today), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        return route;
    }

    [Fact]
    public async Task Handle_WithRouteForToday_AddsOnePendingDeliveryAndCommitsOnce()
    {
        var route = ArrangeRouteForToday();

        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        route.Deliveries.Should().ContainSingle();
        var delivery = route.Deliveries[0];
        delivery.Status.Should().Be(DeliveryStatus.Pending);
        delivery.Package.PackageId.Should().Be("PKG-001");
        delivery.Package.PatientId.Should().Be("PAT-001");
        delivery.Package.LabelData.Should().Be("NTR-001");
        delivery.Address.Description.Should().Be("Av. Banzer 123");
        delivery.Address.PlanarCoordinate.Latitude.Should().Be(-17.80);

        result.RouteId.Should().Be(route.Id);
        result.Deliveries.Should().ContainSingle(d => d.DeliveryId == delivery.Id && d.Status == "pending");

        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(-17.80, null)]
    [InlineData(null, -63.20)]
    public async Task Handle_WithoutCoordinates_FallsBackToTheBranch(double? latitude, double? longitude)
    {
        var route = ArrangeRouteForToday();

        await _handler.Handle(MakeCommand(latitude, longitude), CancellationToken.None);

        var coordinate = route.Deliveries.Single().Address.PlanarCoordinate;
        coordinate.Latitude.Should().Be(BranchLatitude);
        coordinate.Longitude.Should().Be(BranchLongitude);
    }

    [Fact]
    public async Task Handle_WhenNoRouteForToday_ThrowsNotFoundAndDoesNotCommit()
    {
        _routeRepositoryMock
            .Setup(r => r.GetLatestRouteForTodayAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        var act = () => _handler.Handle(MakeCommand(), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<DomainException>();
        exception.Which.Error.Code.Should().Be("Route.NoRouteForToday");
        exception.Which.Error.Type.Should().Be(ErrorType.NotFound);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRouteIsCompleted_ThrowsAndDoesNotCommit()
    {
        var route = RouteTestBuilder.CreateInProgressRoute();
        route.CompleteDelivery(route.Deliveries[0].Id, RouteTestBuilder.MakeConfirmation());
        route.CompleteRoute();
        _routeRepositoryMock
            .Setup(r => r.GetLatestRouteForTodayAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var act = () => _handler.Handle(MakeCommand(), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.Error.Code.Should().Be("Route.CannotAddDelivery");
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
