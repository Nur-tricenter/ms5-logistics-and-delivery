namespace Nurtricenter.Application.Queries.Couriers.GetTodayRoute;

public sealed record CourierTodayRouteResponse(
    Guid CourierId,
    Guid RouteId,
    IReadOnlyList<TodayDeliveryDto> Deliveries);

public sealed record TodayDeliveryDto(
    Guid PackageDeliveryId,
    int SequenceOrder,
    string PatientName,
    string Address,
    string Status,
    string? PatientPhone = null);
