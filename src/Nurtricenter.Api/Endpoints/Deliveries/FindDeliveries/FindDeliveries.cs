namespace Nurtricenter.Api.Endpoints.Deliveries.FindDeliveries;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nurtricenter.Application.Queries.Deliveries.FindDeliveries;

public sealed class FindDeliveries(IMediator mediator) : EndpointBaseAsync
    .WithRequest<FindDeliveriesRequest>
    .WithActionResult<IReadOnlyList<DeliveryLookupDto>>
{
    [HttpGet("/api/v1/deliveries")]
    public override async Task<ActionResult<IReadOnlyList<DeliveryLookupDto>>> HandleAsync(
        [FromQuery] FindDeliveriesRequest request,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            new FindDeliveriesQuery(request.PackageId, request.DeliveryDayId), cancellationToken);

        return Ok(response);
    }
}

public sealed record FindDeliveriesRequest(
    [FromQuery] string? PackageId,
    [FromQuery] int? DeliveryDayId);
