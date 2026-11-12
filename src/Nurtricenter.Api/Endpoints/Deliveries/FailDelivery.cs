namespace Nurtricenter.Api.Endpoints.Deliveries;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nurtricenter.Application.Commands.Deliveries.Fail;

public sealed class FailDelivery(IMediator mediator) : ControllerBase
{
	[HttpPost("/api/v1/routes/{routeId}/deliveries/{deliveryId}/fail")]
	public async Task<ActionResult<FailDeliveryResponse>> HandleAsync(
		[FromRoute] Guid routeId,
		[FromRoute] Guid deliveryId,
		[FromBody] FailDeliveryBodyRequest request,
		CancellationToken cancellationToken)
	{
		var command = new FailDeliveryCommand(routeId, deliveryId, request.Reason);
		var response = await mediator.Send(command, cancellationToken);

		return Ok(response);
	}
}
