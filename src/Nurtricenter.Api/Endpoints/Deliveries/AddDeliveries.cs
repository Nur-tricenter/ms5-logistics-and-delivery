namespace Nurtricenter.Api.Endpoints.Deliveries;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nurtricenter.Application.Commands.Deliveries.Add;

public sealed class AddDeliveries(IMediator mediator) : EndpointBaseAsync
    .WithRequest<IReadOnlyList<AddDeliveryRequestItem>>
    .WithActionResult<AddDeliveriesResponse>
{
    [HttpPost("/api/v1/packages/deliveries")]
    public override async Task<ActionResult<AddDeliveriesResponse>> HandleAsync(
        IReadOnlyList<AddDeliveryRequestItem> request,
        CancellationToken cancellationToken)
    {
        var items = request.Select(r => new AddDeliveryItem(
                r.PatientId,
                r.PackageDetails.PackageId,
                r.PackageDetails.Destination.Address,
                r.PackageDetails.Destination.AddressCoordinates.Lat,
                r.PackageDetails.Destination.AddressCoordinates.Long,
                r.PackageDetails.AdditionalDetails))
            .ToList();

        var command = new AddDeliveriesCommand(items);
        var response = await mediator.Send(command, cancellationToken);

        return Created($"/api/v1/packages/deliveries/{response.RouteId}", response);
    }
}
