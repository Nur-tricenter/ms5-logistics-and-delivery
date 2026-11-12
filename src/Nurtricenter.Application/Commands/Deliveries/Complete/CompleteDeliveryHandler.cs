namespace Nurtricenter.Application.Commands.Deliveries.Complete;

using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using MediatR;
using Nurtricenter.Core.Domain.Delivery.ValueObjects;
using Nurtricenter.Core.Domain.Route.Repositories;

public sealed class CompleteDeliveryHandler : IRequestHandler<CompleteDeliveryCommand, CompleteDeliveryResponse>
{
	private readonly IRouteRepository _routeRepository;
	private readonly IUnitOfWork _unitOfWork;

	public CompleteDeliveryHandler(
		IRouteRepository routeRepository,
		IUnitOfWork unitOfWork)
	{
		_routeRepository = routeRepository;
		_unitOfWork = unitOfWork;
	}

	public async Task<CompleteDeliveryResponse> Handle(
		CompleteDeliveryCommand request,
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

		var confirmation = new DeliveryConfirmation(
			request.DeliveredAt,
			request.EvidencePhotoUrl,
			request.DigitalSignature);

		route.CompleteDelivery(request.DeliveryId, confirmation);

		await _unitOfWork.CommitAsync(cancellationToken);

		var delivery = route.Deliveries.First(d => d.Id == request.DeliveryId);

		return new CompleteDeliveryResponse(
			delivery.Id,
			delivery.RouteId,
			delivery.Status.ToString(),
			new CompleteDeliveryConfirmationDto(
				delivery.Confirmation!.DeliveredAt,
				delivery.Confirmation.EvidencePhotoUrl,
				delivery.Confirmation.DigitalSignature));
	}
}
