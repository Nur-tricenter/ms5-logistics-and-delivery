namespace Nurtricenter.Application.Commands.Deliveries.AddValidatedPackage;

using MediatR;
using Nurtricenter.Application.Commands.Deliveries.Add;

/// <summary>
/// Adds ONE package released by MS3's quality control to the route of its <see cref="DeliveryDate"/>.
/// Event-driven counterpart of <see cref="AddDeliveriesCommand"/>: it tolerates missing coordinates.
/// </summary>
public sealed record AddValidatedPackageCommand(
    string PackageId,
    string PatientId,
    string PatientName,
    DateOnly DeliveryDate,
    string Address,
    double? Latitude,
    double? Longitude,
    string LabelData,
    int? DeliveryDayId = null,
    string? PatientPhone = null) : IRequest<AddDeliveriesResponse>;
