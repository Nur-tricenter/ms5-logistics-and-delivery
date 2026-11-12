namespace Nurtricenter.Application.Commands.Deliveries.Fail;

using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using MediatR;
using Nurtricenter.Core.Domain.Route.Repositories;

public sealed class FailDeliveryHandler : IRequestHandler<FailDeliveryCommand, FailDeliveryResponse>
{
	private readonly IRouteRepository _routeRepository;
	private readonly IUnitOfWork _unitOfWork;

	public FailDeliveryHandler(
		IRouteRepository routeRepository,
		IUnitOfWork unitOfWork)
	{
		_routeRepository = routeRepository;
		_unitOfWork = unitOfWork;
	}

	public async Task<FailDeliveryResponse> Handle(
		FailDeliveryCommand request,
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

		route.FailDelivery(request.DeliveryId, request.Reason);

		await _unitOfWork.CommitAsync(cancellationToken);

		var delivery = route.Deliveries.First(d => d.Id == request.DeliveryId);

		return new FailDeliveryResponse(
			delivery.Id,
			delivery.RouteId,
			delivery.Status.ToString(),
			delivery.FailureReason!);
	}
}
