namespace Nurtricenter.Api.Endpoints.Routes;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nurtricenter.Application.Commands.Routes.Start;

public sealed class StartRoute(IMediator mediator) : EndpointBaseAsync
	.WithRequest<StartRouteRequest>
	.WithActionResult<StartRouteResponse>
{
	[HttpPost("/api/v1/routes/{routeId}/start")]
	public override async Task<ActionResult<StartRouteResponse>> HandleAsync(
		[FromRoute] StartRouteRequest request,
		CancellationToken cancellationToken)
	{
		var command = new StartRouteCommand(request.RouteId);
		var response = await mediator.Send(command, cancellationToken);

		return Ok(response);
	}
}
