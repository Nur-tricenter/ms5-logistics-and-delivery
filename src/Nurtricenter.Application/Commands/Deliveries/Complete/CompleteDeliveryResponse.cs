namespace Nurtricenter.Application.Commands.Deliveries.Complete;

public sealed record CompleteDeliveryResponse(
	Guid DeliveryId,
	Guid RouteId,
	string Status,
	CompleteDeliveryConfirmationDto Confirmation);

public sealed record CompleteDeliveryConfirmationDto(
	DateTime DeliveredAt,
	string EvidencePhotoUrl,
	string DigitalSignature);
