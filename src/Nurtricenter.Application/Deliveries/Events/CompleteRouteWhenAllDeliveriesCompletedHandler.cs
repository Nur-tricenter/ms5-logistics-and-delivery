namespace Nurtricenter.Application.Deliveries.Events;

using Joseco.DDD.Core.Abstractions;
using MediatR;
using Nurtricenter.Core.Domain.Delivery.Events;
using Nurtricenter.Core.Domain.Route.Enums;
using Nurtricenter.Core.Domain.Route.Repositories;

public sealed class CompleteRouteWhenAllDeliveriesCompletedHandler
	: INotificationHandler<DeliveryCompletedEvent>,
	  INotificationHandler<DeliveryFailedEvent>
{
	private readonly IRouteRepository _routeRepository;
	private readonly IUnitOfWork _unitOfWork;

	public CompleteRouteWhenAllDeliveriesCompletedHandler(
		IRouteRepository routeRepository,
		IUnitOfWork unitOfWork)
	{
		_routeRepository = routeRepository;
		_unitOfWork = unitOfWork;
	}

	public async Task Handle(
		DeliveryCompletedEvent notification,
		CancellationToken cancellationToken)
	{
		await CompleteRouteIfAllTerminalAsync(notification.RouteId, cancellationToken);
	}

	public async Task Handle(
		DeliveryFailedEvent notification,
		CancellationToken cancellationToken)
	{
		await CompleteRouteIfAllTerminalAsync(notification.RouteId, cancellationToken);
	}

	private async Task CompleteRouteIfAllTerminalAsync(Guid routeId, CancellationToken cancellationToken)
	{
		var route = await _routeRepository.GetByIdAsync(routeId, readOnly: false);

		if (route is null)
			return;

		if (route.Status == RouteStatus.Completed || route.Status == RouteStatus.Cancelled)
			return;

		if (route.Deliveries.All(d => d.IsTerminal))
		{
			route.CompleteRoute();
			await _unitOfWork.CommitAsync(cancellationToken);
		}
	}
}
