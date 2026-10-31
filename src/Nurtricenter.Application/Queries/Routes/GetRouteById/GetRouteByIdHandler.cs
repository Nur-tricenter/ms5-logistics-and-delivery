namespace Nurtricenter.Application.Queries.Routes.GetRouteById;

using Joseco.DDD.Core.Results;
using MediatR;
using Nurtricenter.Core.Domain.Route.Repositories;

/// <summary>Any route by id, whatever its date (route/today only shows today's), with its deliveries.</summary>
public sealed class GetRouteByIdHandler : IRequestHandler<GetRouteByIdQuery, RouteDetailDto>
{
    private readonly IRouteRepository _routeRepository;

    public GetRouteByIdHandler(IRouteRepository routeRepository)
    {
        _routeRepository = routeRepository;
    }

    public async Task<RouteDetailDto> Handle(GetRouteByIdQuery request, CancellationToken cancellationToken)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, readOnly: true);

        if (route is null)
        {
            throw new DomainException(
                Error.NotFound("Route.NotFound", "Route '{routeId}' was not found.", request.RouteId.ToString()));
        }

        return route.ToDetailDto();
    }
}
