namespace Nurtricenter.Application.Commands.Routes.Start;

using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using MediatR;
using Microsoft.Extensions.Options;
using Nurtricenter.Core.Domain.Delivery.ValueObjects;
using Nurtricenter.Core.Domain.Route.Repositories;
using Nurtricenter.Core.Options;

public sealed class StartRouteHandler : IRequestHandler<StartRouteCommand, StartRouteResponse>
{
	private readonly IRouteRepository _routeRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly BranchCoordinatesOptions _branchCoordinates;

	public StartRouteHandler(
		IRouteRepository routeRepository,
		IUnitOfWork unitOfWork,
		IOptions<BranchCoordinatesOptions> branchCoordinates)
	{
		_routeRepository = routeRepository;
		_unitOfWork = unitOfWork;
		_branchCoordinates = branchCoordinates.Value;
	}

	public async Task<StartRouteResponse> Handle(
		StartRouteCommand request,
		CancellationToken cancellationToken)
	{
		var route = await _routeRepository.GetByIdAsync(request.RouteId, readOnly: false);

		if (route is null)
		{
			throw new DomainException(
				Error.NotFound(
					"Route.NotFound",
					"Route '{routeId}' was not found.",
					request.RouteId.ToString()));
		}

		var startingPoint = new Coordinate(_branchCoordinates.Latitude, _branchCoordinates.Longitude);
		route.StartRoute(startingPoint);

		await _unitOfWork.CommitAsync(cancellationToken);

		var deliveries = route.Deliveries
			.OrderBy(d => d.SequenceOrder)
			.Select(d => new StartRouteDeliveryDto(
				d.Id,
				d.SequenceOrder!.Value,
				d.Address.Description))
			.ToList();

		return new StartRouteResponse(route.Id, route.CourierId, deliveries);
	}
}
