namespace Nurtricenter.Application.Commands.Deliveries.Fail;

using MediatR;

public sealed record FailDeliveryCommand(
	Guid RouteId,
	Guid DeliveryId,
	string Reason) : IRequest<FailDeliveryResponse>;
