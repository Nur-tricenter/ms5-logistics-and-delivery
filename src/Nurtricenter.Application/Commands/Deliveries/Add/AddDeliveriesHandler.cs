namespace Nurtricenter.Application.Commands.Deliveries.Add;

using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using MediatR;
using Nurtricenter.Core.Domain.Delivery.ValueObjects;
using Nurtricenter.Core.Domain.Route.Repositories;

public sealed class AddDeliveriesHandler : IRequestHandler<AddDeliveriesCommand, AddDeliveriesResponse>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddDeliveriesHandler(
        IRouteRepository routeRepository,
        IUnitOfWork unitOfWork)
    {
        _routeRepository = routeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AddDeliveriesResponse> Handle(
        AddDeliveriesCommand request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var route = await _routeRepository.GetLatestRouteForDateAsync(today, cancellationToken);

        if (route is null)
        {
            throw new DomainException(
                Error.NotFound(
                    "Route.NoRouteForToday",
                    "No route found for today ({date}). Create a route first.",
                    today.ToString("yyyy-MM-dd")));
        }

        foreach (var item in request.Items)
        {
            var coordinate = new Coordinate(item.Latitude, item.Longitude);
            var address = new DeliveryAddress(item.Address, coordinate);
            var package = new ValidatedPackage(item.PackageId, item.PatientId, item.PatientName, item.AdditionalDetails);

            route.AddDelivery(package, address);
        }

        await _unitOfWork.CommitAsync(cancellationToken);

        var responseDeliveries = route.Deliveries
            .TakeLast(request.Items.Count)
            .Select(d => new DeliveryItemResponse(
                d.Package.PatientId,
                d.Id,
                d.Status.ToString().ToLowerInvariant()))
            .ToList();

        return new AddDeliveriesResponse(route.Id, responseDeliveries);
    }
}
