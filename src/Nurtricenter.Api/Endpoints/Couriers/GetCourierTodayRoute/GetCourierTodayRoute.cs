namespace Nurtricenter.Api.Endpoints.Couriers.GetCourierTodayRoute;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nurtricenter.Application.Queries.Couriers.GetTodayRoute;

public sealed class GetCourierTodayRoute(IMediator mediator) : EndpointBaseAsync
	.WithRequest<GetCourierTodayRouteRequest>
	.WithActionResult<CourierTodayRouteResponse>
{
	[HttpGet("/api/v1/couriers/{courierId}/route/today")]
	public override async Task<ActionResult<CourierTodayRouteResponse>> HandleAsync(
		[FromRoute] GetCourierTodayRouteRequest request,
		CancellationToken cancellationToken)
	{
		var query = new GetCourierTodayRouteQuery(request.CourierId);
		var response = await mediator.Send(query, cancellationToken);

		return Ok(response);
	}
}
