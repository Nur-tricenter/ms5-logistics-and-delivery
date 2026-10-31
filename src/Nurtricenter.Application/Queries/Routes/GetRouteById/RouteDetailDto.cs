namespace Nurtricenter.Application.Queries.Routes.GetRouteById;

public sealed record RouteDetailDto(
    Guid Id,
    Guid CourierId,
    DateOnly ScheduledDate,
    string Status,
    DateTime CreatedAt,
    IReadOnlyList<RouteDeliveryDto> Deliveries);

/// <summary>A delivery as stored in MS5, with the ids that link it to MS3 (package) and MS4 (delivery day).</summary>
public sealed record RouteDeliveryDto(
    Guid DeliveryId,
    Guid RouteId,
    int? SequenceOrder,
    string Status,
    string PackageId,
    int? DeliveryDayId,
    string PatientId,
    string PatientName,
    string? PatientPhone,
    string Address,
    double Latitude,
    double Longitude,
    DateTime? DeliveredAt,
    string? EvidencePhotoUrl,
    string? FailureReason,
    DateTime CreatedAt);
