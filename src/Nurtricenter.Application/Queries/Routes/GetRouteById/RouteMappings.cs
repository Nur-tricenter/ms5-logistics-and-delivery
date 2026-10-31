namespace Nurtricenter.Application.Queries.Routes.GetRouteById;

using Nurtricenter.Core.Domain.Route;
using DeliveryEntity = Nurtricenter.Core.Domain.Delivery.Delivery;

internal static class RouteMappings
{
    public static RouteDetailDto ToDetailDto(this Route route) =>
        new(
            route.Id,
            route.CourierId,
            route.ScheduledDate,
            route.Status.ToString(),
            route.CreatedAt,
            route.Deliveries
                .OrderBy(d => d.SequenceOrder ?? int.MaxValue)
                .ThenBy(d => d.CreatedAt)
                .Select(d => d.ToDto(route.Id))
                .ToList());

    public static RouteDeliveryDto ToDto(this DeliveryEntity delivery, Guid routeId) =>
        new(
            delivery.Id,
            routeId,
            delivery.SequenceOrder,
            delivery.Status.ToString(),
            delivery.Package.PackageId,
            delivery.Package.DeliveryDayId,
            delivery.Package.PatientId,
            delivery.Package.PatientName,
            delivery.Package.PatientPhone,
            delivery.Address.Description,
            delivery.Address.PlanarCoordinate.Latitude,
            delivery.Address.PlanarCoordinate.Longitude,
            delivery.Confirmation?.DeliveredAt,
            delivery.Confirmation?.EvidencePhotoUrl,
            delivery.FailureReason,
            delivery.CreatedAt);
}
