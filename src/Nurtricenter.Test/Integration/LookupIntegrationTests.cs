namespace Nurtricenter.Test.Integration;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nurtricenter.Application.Queries.Deliveries.FindDeliveries;
using Nurtricenter.Application.Queries.Routes.GetRouteById;
using Nurtricenter.Core.Domain.Delivery.ValueObjects;
using Nurtricenter.Core.Domain.Route;
using Nurtricenter.Infrastructure.Data;

/// <summary>GET /api/v1/routes/{id} and GET /api/v1/deliveries (cross-service gap X-17).</summary>
[Collection("Integration")]
public class LookupIntegrationTests : BaseIntegrationTest
{
    public LookupIntegrationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    private async Task<Route> SeedRouteAsync(DateOnly date)
    {
        var route = new Route(Guid.NewGuid(), Guid.NewGuid(), date);
        route.AddDelivery(
            new ValidatedPackage("ms3-package-a", "patient-a", "Ana", "label", 41),
            new DeliveryAddress("Calle 1", new Coordinate(-17.7, -63.1)));
        route.AddDelivery(
            new ValidatedPackage("ms3-package-b", "patient-b", "Beto", "label", 42),
            new DeliveryAddress("Calle 2", new Coordinate(-17.8, -63.2)));

        await ExecuteInScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.Routes.Add(route);
            await db.SaveChangesAsync();
        });

        return route;
    }

    [Fact]
    public async Task GetRouteById_ForAFutureRoute_ReturnsItWithDeliveries()
    {
        await ClearDatabaseAsync();
        var route = await SeedRouteAsync(DateOnly.FromDateTime(DateTime.Today).AddDays(1));

        var response = await Client.GetAsync($"/api/v1/routes/{route.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RouteDetailDto>();
        body!.Id.Should().Be(route.Id);
        body.Deliveries.Should().HaveCount(2);
        body.Deliveries.Select(d => d.PackageId).Should().BeEquivalentTo("ms3-package-a", "ms3-package-b");
    }

    [Fact]
    public async Task GetRouteById_WhenMissing_Returns404()
    {
        var response = await Client.GetAsync($"/api/v1/routes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FindDeliveries_ByPackageIdOrDay_ReturnsTheDeliveryAndItsRoute()
    {
        await ClearDatabaseAsync();
        var route = await SeedRouteAsync(DateOnly.FromDateTime(DateTime.Today).AddDays(1));

        var byPackage = await Client.GetFromJsonAsync<List<DeliveryLookupDto>>("/api/v1/deliveries?packageId=ms3-package-b");
        var byDay = await Client.GetFromJsonAsync<List<DeliveryLookupDto>>("/api/v1/deliveries?deliveryDayId=41");

        byPackage.Should().ContainSingle().Which.Delivery.DeliveryDayId.Should().Be(42);
        byPackage![0].Delivery.RouteId.Should().Be(route.Id);
        byDay.Should().ContainSingle().Which.Delivery.PackageId.Should().Be("ms3-package-a");
    }

    [Fact]
    public async Task FindDeliveries_WithoutFilters_Returns400()
    {
        var response = await Client.GetAsync("/api/v1/deliveries");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
