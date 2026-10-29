namespace Nurtricenter.Application.Commands.Deliveries.Add;

public sealed record AddDeliveryItem(
    string PatientId,
    string PatientName,
    string PackageId,
    string Address,
    double Latitude,
    double Longitude,
    string AdditionalDetails);
