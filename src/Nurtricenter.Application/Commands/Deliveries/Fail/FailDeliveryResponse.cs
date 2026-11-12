namespace Nurtricenter.Application.Commands.Deliveries.Fail;

public sealed record FailDeliveryResponse(
	Guid DeliveryId,
	Guid RouteId,
	string Status,
	string FailureReason);
