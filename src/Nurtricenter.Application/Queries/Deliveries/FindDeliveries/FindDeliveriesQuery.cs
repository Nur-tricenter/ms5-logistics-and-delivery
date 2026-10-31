namespace Nurtricenter.Application.Queries.Deliveries.FindDeliveries;

using MediatR;
using Nurtricenter.Application.Queries.Routes.GetRouteById;

/// <summary>
/// Deliveries carrying an MS3 package (<paramref name="PackageId"/>) and/or MS4 delivery day
/// (<paramref name="DeliveryDayId"/>). At least one filter is required.
/// </summary>
public sealed record FindDeliveriesQuery(string? PackageId, int? DeliveryDayId) : IRequest<IReadOnlyList<DeliveryLookupDto>>;

public sealed record DeliveryLookupDto(
    RouteDeliveryDto Delivery,
    Guid CourierId,
    DateOnly ScheduledDate,
    string RouteStatus);
