namespace Nurtricenter.Test.Deliveries.Application;

using FluentAssertions;
using Joseco.DDD.Core.Results;
using Moq;
using Nurtricenter.Application.Queries.Deliveries.FindDeliveries;
using Nurtricenter.Application.Queries.Routes.GetRouteById;
using Nurtricenter.Core.Domain.Delivery.ValueObjects;
using Nurtricenter.Core.Domain.Route;
using Nurtricenter.Core.Domain.Route.Repositories;
using Nurtricenter.Test.Helpers;

/// <summary>Lookups for following a case end to end (cross-service gap X-17).</summary>
public class LookupHandlersTests
{
    private readonly Mock<IRouteRepository> _routes = new();

    private static Route RouteWith(params (string PackageId, int? DayId)[] packages)
    {
        var route = new Route(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 11, 1));
        foreach (var (packageId, dayId) in packages)
        {
            route.AddDelivery(
                new ValidatedPackage(packageId, "patient-1", "Juan Perez", "label", dayId, "70000000"),
                new DeliveryAddress("Av. Banzer 123", new Coordinate(-17.78, -63.18)));
        }

        return route;
    }

    [Fact]
    public async Task GetRouteById_ReturnsRouteWithEveryDelivery()
    {
        var route = RouteWith(("pkg-1", 11), ("pkg-2", 12));
        _routes.Setup(r => r.GetByIdAsync(route.Id, true)).ReturnsAsync(route);

        var result = await new GetRouteByIdHandler(_routes.Object).Handle(new GetRouteByIdQuery(route.Id), CancellationToken.None);

        result.Id.Should().Be(route.Id);
        result.ScheduledDate.Should().Be(new DateOnly(2026, 11, 1));
        result.Status.Should().Be("Pending");
        result.Deliveries.Should().HaveCount(2);
        result.Deliveries.Select(d => d.DeliveryDayId).Should().BeEquivalentTo(new int?[] { 11, 12 });
        result.Deliveries.Should().AllSatisfy(d =>
        {
            d.RouteId.Should().Be(route.Id);
            d.PatientPhone.Should().Be("70000000");
            d.Latitude.Should().Be(-17.78);
        });
    }

    [Fact]
    public async Task GetRouteById_WhenMissing_ThrowsNotFound()
    {
        _routes.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), true)).ReturnsAsync((Route?)null);

        var act = () => new GetRouteByIdHandler(_routes.Object).Handle(new GetRouteByIdQuery(Guid.NewGuid()), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task FindDeliveries_ByPackageId_ReturnsOnlyTheMatchingDelivery_WithItsRoute()
    {
        var route = RouteWith(("pkg-1", 11), ("pkg-2", 12));
        _routes.Setup(r => r.GetByDeliveryPackageAsync("pkg-2", null, It.IsAny<CancellationToken>())).ReturnsAsync([route]);

        var result = await new FindDeliveriesHandler(_routes.Object).Handle(new FindDeliveriesQuery(" pkg-2 ", null), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Delivery.PackageId.Should().Be("pkg-2");
        result[0].Delivery.DeliveryDayId.Should().Be(12);
        result[0].CourierId.Should().Be(route.CourierId);
        result[0].RouteStatus.Should().Be("Pending");
    }

    [Fact]
    public async Task FindDeliveries_ByDeliveryDayId_FiltersOnTheDay()
    {
        var route = RouteWith(("pkg-1", 11), ("pkg-2", 12));
        _routes.Setup(r => r.GetByDeliveryPackageAsync(null, 11, It.IsAny<CancellationToken>())).ReturnsAsync([route]);

        var result = await new FindDeliveriesHandler(_routes.Object).Handle(new FindDeliveriesQuery(null, 11), CancellationToken.None);

        result.Should().ContainSingle().Which.Delivery.PackageId.Should().Be("pkg-1");
    }

    [Fact]
    public async Task FindDeliveries_WithNoMatch_ReturnsEmpty()
    {
        _routes.Setup(r => r.GetByDeliveryPackageAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await new FindDeliveriesHandler(_routes.Object).Handle(new FindDeliveriesQuery("nope", null), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindDeliveries_WithoutAnyFilter_ThrowsValidation()
    {
        var act = () => new FindDeliveriesHandler(_routes.Object).Handle(new FindDeliveriesQuery("  ", null), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.Error.Type.Should().Be(ErrorType.Validation);
        _routes.Verify(r => r.GetByDeliveryPackageAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
