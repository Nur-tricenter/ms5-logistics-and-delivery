namespace Nurtricenter.Application.Queries.Couriers.GetTodayRoute;

using Joseco.DDD.Core.Results;
using MediatR;
using Nurtricenter.Core.Domain.Courier.Repositories;
using Nurtricenter.Core.Domain.Route.Repositories;

public sealed class GetCourierTodayRouteHandler : IRequestHandler<GetCourierTodayRouteQuery, CourierTodayRouteResponse>
{
    private readonly ICourierRepository _courierRepository;
    private readonly IRouteRepository _routeRepository;

    public GetCourierTodayRouteHandler(
        ICourierRepository courierRepository,
        IRouteRepository routeRepository)
    {
        _courierRepository = courierRepository;
        _routeRepository = routeRepository;
    }

    public async Task<CourierTodayRouteResponse> Handle(
        GetCourierTodayRouteQuery request,
        CancellationToken cancellationToken)
    {
        var courier = await _courierRepository.GetByIdAsync(request.CourierId, cancellationToken);

        if (courier is null)
        {
            throw new DomainException(
                Error.NotFound(
                    "Courier.NotFound",
                    "Courier '{courierId}' was not found.",
                    request.CourierId.ToString()));
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var route = await _routeRepository.GetByCourierAndDateAsync(request.CourierId, today, cancellationToken);

        if (route is null)
        {
            throw new DomainException(
                Error.NotFound(
                    "Route.NoRouteForToday",
                    "No route found for courier '{courierId}' on {date}.",
                    request.CourierId.ToString(),
                    today.ToString("yyyy-MM-dd")));
        }

        // Deliveries stored before the patient name was captured have an empty one; show the id instead.
        var deliveryDtos = route.Deliveries
            .Select((d, index) => new TodayDeliveryDto(
                d.Id,
                d.SequenceOrder ?? index + 1,
                string.IsNullOrWhiteSpace(d.Package.PatientName) ? d.Package.PatientId : d.Package.PatientName,
                d.Address.Description,
                d.Status.ToString()))
            .ToList();

        return new CourierTodayRouteResponse(request.CourierId, route.Id, deliveryDtos);
    }
}
