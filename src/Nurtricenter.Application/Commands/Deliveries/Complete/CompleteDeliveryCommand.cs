namespace Nurtricenter.Application.Commands.Deliveries.Complete;

using MediatR;

public sealed record CompleteDeliveryCommand(
	Guid RouteId,
	Guid DeliveryId,
	DateTime DeliveredAt,
	string EvidencePhotoUrl,
	string DigitalSignature) : IRequest<CompleteDeliveryResponse>;
