namespace Nurtricenter.Api.Endpoints.Couriers.GetCourierTodayRoute;

using Microsoft.AspNetCore.Mvc;

public sealed record GetCourierTodayRouteRequest(
	[FromRoute] Guid CourierId);
