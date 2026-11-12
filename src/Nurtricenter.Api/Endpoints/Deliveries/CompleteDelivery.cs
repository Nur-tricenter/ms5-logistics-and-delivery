namespace Nurtricenter.Api.Endpoints.Deliveries;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nurtricenter.Application.Commands.Deliveries.Complete;

public sealed class CompleteDelivery(IMediator mediator) : ControllerBase
{
	[HttpPost("/api/v1/routes/{routeId}/deliveries/{deliveryId}/deliver")]
	public async Task<ActionResult<CompleteDeliveryResponse>> HandleAsync(
		[FromRoute] Guid routeId,
		[FromRoute] Guid deliveryId,
		[FromBody] CompleteDeliveryBodyRequest request,
		CancellationToken cancellationToken)
	{
		var command = new CompleteDeliveryCommand(
			routeId,
			deliveryId,
			request.DeliveredAt,
			request.EvidencePhotoUrl,
			request.DigitalSignature);

		var response = await mediator.Send(command, cancellationToken);

		return Ok(response);
	}
}
