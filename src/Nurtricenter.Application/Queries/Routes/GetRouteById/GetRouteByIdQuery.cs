namespace Nurtricenter.Application.Queries.Routes.GetRouteById;

using MediatR;

public sealed record GetRouteByIdQuery(Guid RouteId) : IRequest<RouteDetailDto>;
