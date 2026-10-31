namespace Nurtricenter.Application.Queries.Deliveries.FindDeliveries;

using Joseco.DDD.Core.Results;
using MediatR;
using Nurtricenter.Application.Queries.Routes.GetRouteById;
using Nurtricenter.Core.Domain.Route.Repositories;

/// <summary>
/// Follows an MS3 package or an MS4 delivery day to the MS5 delivery (and route) that carries it
/// (cross-service gap X-17). Newest route first; an unknown package is an empty list.
/// </summary>
public sealed class FindDeliveriesHandler : IRequestHandler<FindDeliveriesQuery, IReadOnlyList<DeliveryLookupDto>>
{
    private readonly IRouteRepository _routeRepository;

    public FindDeliveriesHandler(IRouteRepository routeRepository)
    {
        _routeRepository = routeRepository;
    }

    public async Task<IReadOnlyList<DeliveryLookupDto>> Handle(FindDeliveriesQuery request, CancellationToken cancellationToken)
    {
        var packageId = string.IsNullOrWhiteSpace(request.PackageId) ? null : request.PackageId.Trim();

        if (packageId is null && request.DeliveryDayId is null)
        {
            throw new DomainException(new Error(
                "Delivery.LookupFilterRequired",
                "Pass packageId and/or deliveryDayId.",
                ErrorType.Validation));
        }

        var routes = await _routeRepository.GetByDeliveryPackageAsync(packageId, request.DeliveryDayId, cancellationToken);

        return routes
            .SelectMany(route => route.Deliveries
                .Where(d => (packageId is null || d.Package.PackageId == packageId)
                            && (request.DeliveryDayId is null || d.Package.DeliveryDayId == request.DeliveryDayId))
                .Select(d => new DeliveryLookupDto(d.ToDto(route.Id), route.CourierId, route.ScheduledDate, route.Status.ToString())))
            .ToList();
    }
}
