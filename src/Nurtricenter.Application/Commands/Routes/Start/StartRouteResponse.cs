namespace Nurtricenter.Application.Commands.Routes.Start;

public sealed record StartRouteResponse(
	Guid RouteId,
	Guid CourierId,
	IReadOnlyList<StartRouteDeliveryDto> Deliveries);

public sealed record StartRouteDeliveryDto(
	Guid DeliveryId,
	int SequenceOrder,
	string Address);
