namespace Nurtricenter.Application.Commands.Deliveries.AddValidatedPackage;

using MediatR;
using Nurtricenter.Application.Commands.Deliveries.Add;

/// <summary>
/// Adds ONE package released by MS3's quality control to today's route. Event-driven counterpart of
/// <see cref="AddDeliveriesCommand"/>: it skips the ClinicService patient check (MS3 already validated
/// the package) and tolerates missing coordinates.
/// </summary>
public sealed record AddValidatedPackageCommand(
    string PackageId,
    string PatientId,
    string Address,
    double? Latitude,
    double? Longitude,
    string LabelData) : IRequest<AddDeliveriesResponse>;
