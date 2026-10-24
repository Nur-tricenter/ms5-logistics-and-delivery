namespace Nurtricenter.Test.Helpers;

using Nurtricenter.Core.Domain.Delivery.ValueObjects;
using Nurtricenter.Core.Domain.Route;

/// <summary>
/// Shared factory for building Route aggregates in specific states
/// without duplicating setup code across test classes.
/// </summary>
public static class RouteTestBuilder
{
    public static readonly double DefaultLat = 10.0;
    public static readonly double DefaultLon = 20.0;

    /// <summary>Creates a fresh Pending Route with no deliveries.</summary>
    public static Route CreatePendingRoute(Guid? courierId = null)
        => new(
            Guid.NewGuid(),
            courierId ?? Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today));

    /// <summary>
    /// Creates a Pending Route, adds <paramref name="deliveryCount"/> deliveries,
    /// then calls StartRoute so it transitions to InProgress (all deliveries -> InProgress).
    /// </summary>
    public static Route CreateInProgressRoute(int deliveryCount = 1, Guid? courierId = null)
    {
        var route = CreatePendingRoute(courierId);
        AddDeliveries(route, deliveryCount);
        route.StartRoute(new Coordinate(0.0, 0.0));
        return route;
    }

    /// <summary>Adds <paramref name="count"/> deliveries to an existing route.</summary>
    public static void AddDeliveries(Route route, int count = 1, int startIndex = 1)
    {
        for (int i = startIndex; i < startIndex + count; i++)
        {
            var package = new ValidatedPackage($"PKG-{i:000}", $"PAT-{i:000}", $"label-{i}");
            var address = new DeliveryAddress($"Street {i}", new Coordinate(i * 1.0, i * 2.0));
            route.AddDelivery(package, address);
        }
    }

    /// <summary>Creates a valid DeliveryConfirmation value object.</summary>
    public static DeliveryConfirmation MakeConfirmation(DateTime? at = null)
        => new(
            at ?? DateTime.UtcNow,
            "https://evidence.example.com/photo.jpg",
            "valid-digital-signature");
}
