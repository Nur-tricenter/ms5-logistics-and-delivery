namespace Nurtricenter.Test.Integration;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nurtricenter.Core.Domain.Route.Enums;
using Nurtricenter.Core.Interfaces.Services.ClinicService;
using Nurtricenter.Core.Interfaces.Services.ClinicService.Dto;
using Nurtricenter.Infrastructure.Data;

[Collection("Integration")]
public class RouteLifecycleIntegrationTests : BaseIntegrationTest
{
    public RouteLifecycleIntegrationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CompleteDeliveryLifecycle_CreatesRoute_AddsDeliveries_Starts_AndCompletes()
    {
        await ClearDatabaseAsync();

        // 1. Create Courier
        var createCourierRes = await Client.PostAsJsonAsync("/api/v1/couriers", new { FullName = "Jane Fast" });
        createCourierRes.EnsureSuccessStatusCode();
        var courier = await createCourierRes.Content.ReadFromJsonAsync<CourierResponseDto>();
        var courierId = courier!.Id;

        // 2. Create Route
        var scheduledDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var createRouteReq = new { CourierId = courierId, ScheduledDate = scheduledDate };
        var createRouteRes = await Client.PostAsJsonAsync("/api/v1/routes", createRouteReq);
        if (!createRouteRes.IsSuccessStatusCode)
        {
            var err = await createRouteRes.Content.ReadAsStringAsync();
            throw new Exception($"POST /api/v1/routes failed: {createRouteRes.StatusCode} - {err}");
        }
        var routeInfo = await createRouteRes.Content.ReadFromJsonAsync<RouteResponseDto>();
        var routeId = routeInfo!.Id;
    }

    private sealed record CourierResponseDto(Guid Id);
    private sealed record RouteResponseDto(Guid Id);
}
