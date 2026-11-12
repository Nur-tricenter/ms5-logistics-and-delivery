namespace Nurtricenter.Application.Commands.Routes.Create;

using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using MediatR;
using Nurtricenter.Core.Domain.Courier.Repositories;
using Nurtricenter.Core.Domain.Route;
using Nurtricenter.Core.Domain.Route.Repositories;

public sealed class CreateRouteHandler : IRequestHandler<CreateRouteCommand, RouteResponse>
{
	private readonly ICourierRepository _courierRepository;
	private readonly IRouteRepository _routeRepository;
	private readonly IUnitOfWork _unitOfWork;

	public CreateRouteHandler(
		ICourierRepository courierRepository,
		IRouteRepository routeRepository,
		IUnitOfWork unitOfWork)
	{
		_courierRepository = courierRepository;
		_routeRepository = routeRepository;
		_unitOfWork = unitOfWork;
	}

	public async Task<RouteResponse> Handle(CreateRouteCommand request, CancellationToken cancellationToken)
	{
		var courier = await _courierRepository.GetByIdAsync(request.CourierId, cancellationToken);

		if (courier is null)
			throw new DomainException(
				Error.NotFound(
					"Courier.NotFound",
					"Courier '{courierId}' was not found.",
					request.CourierId.ToString()));

		var route = new Route(Guid.NewGuid(), request.CourierId, request.ScheduledDate);

		await _routeRepository.AddAsync(route);
		await _unitOfWork.CommitAsync(cancellationToken);

		return new RouteResponse(route.Id, route.CourierId, route.ScheduledDate, route.Status);
	}
}
