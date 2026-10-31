namespace Nurtricenter.Api.Endpoints.Routes.GetRouteById;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nurtricenter.Application.Queries.Routes.GetRouteById;

public sealed class GetRouteById(IMediator mediator) : EndpointBaseAsync
    .WithRequest<GetRouteByIdRequest>
    .WithActionResult<RouteDetailDto>
{
    [HttpGet("/api/v1/routes/{routeId:guid}")]
    public override async Task<ActionResult<RouteDetailDto>> HandleAsync(
        [FromRoute] GetRouteByIdRequest request,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetRouteByIdQuery(request.RouteId), cancellationToken);

        return Ok(response);
    }
}

public sealed record GetRouteByIdRequest([FromRoute] Guid RouteId);
