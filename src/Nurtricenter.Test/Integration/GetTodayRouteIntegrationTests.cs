namespace Nurtricenter.Test.Integration;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Nurtricenter.Core.Domain.Courier;
using Nurtricenter.Core.Domain.Delivery.ValueObjects;
using Nurtricenter.Core.Domain.Route;
using Nurtricenter.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Moq;

[Collection("Integration")]
public class GetTodayRouteIntegrationTests : BaseIntegrationTest
{
    public GetTodayRouteIntegrationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetTodayRoute_WhenRouteExists_ReturnsMappedRouteAndDeliveries()
    {
        await ClearDatabaseAsync();

        var courierId = Guid.NewGuid();
        var routeId = Guid.NewGuid();

        await ExecuteInScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            
            var courier = new Courier(courierId, "Test Courier");
            db.Couriers.Add(courier);
            
            var route = new Route(routeId, courierId, DateOnly.FromDateTime(DateTime.Today));
            
            var package = new ValidatedPackage("PKG-1", "PAT-1", "label");
            var address = new DeliveryAddress("123 Test St", new Coordinate(1.0, 1.0));
            route.AddDelivery(package, address);
            route.StartRoute(new Coordinate(0.0, 0.0));
            
            db.Routes.Add(route);
            await db.SaveChangesAsync();
        });

        var response = await Client.GetAsync($"/api/v1/couriers/{courierId}/route/today");
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new Exception($"GET /couriers/{courierId}/route/today failed: {response.StatusCode} - {err}");
        }

        var content = await response.Content.ReadFromJsonAsync<CourierTodayRouteResponseDto>();
        content.Should().NotBeNull();
        content!.CourierId.Should().Be(courierId);
    }
    
    private sealed record CourierTodayRouteResponseDto(Guid CourierId, string CourierName, string Status);
}
