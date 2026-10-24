namespace Nurtricenter.Api.Endpoints.Routes;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nurtricenter.Application.Commands.Routes.Create;

public sealed class CreateRoute(IMediator mediator) : EndpointBaseAsync
    .WithRequest<CreateRouteRequest>
    .WithActionResult<RouteResponse>
{
    [HttpPost("/api/v1/routes")]
    public override async Task<ActionResult<RouteResponse>> HandleAsync(
        CreateRouteRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateRouteCommand(request.CourierId, request.ScheduledDate);
        var response = await mediator.Send(command, cancellationToken);

        return Created($"/api/v1/routes/{response.Id}", response);
    }
}
