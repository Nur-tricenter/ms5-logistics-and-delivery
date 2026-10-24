namespace Nurtricenter.Api.Endpoints.Couriers.CreateCourier;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nurtricenter.Application.Commands.Couriers.Create;

public sealed class CreateCourier(IMediator mediator) : EndpointBaseAsync
    .WithRequest<CreateCourierRequest>
    .WithActionResult<CourierResponse>
{
    [HttpPost("/api/v1/couriers")]
    public override async Task<ActionResult<CourierResponse>> HandleAsync(
        CreateCourierRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCourierCommand(request.FullName);
        var response = await mediator.Send(command, cancellationToken);

        return Created($"/api/v1/couriers/{response.Id}", response);
    }
}
