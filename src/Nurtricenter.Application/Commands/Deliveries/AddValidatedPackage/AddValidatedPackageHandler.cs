namespace Nurtricenter.Application.Commands.Deliveries.AddValidatedPackage;

using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nurtricenter.Application.Commands.Deliveries.Add;
using Nurtricenter.Core.Domain.Delivery.ValueObjects;
using Nurtricenter.Core.Domain.Route.Repositories;
using Nurtricenter.Core.Options;

public sealed class AddValidatedPackageHandler : IRequestHandler<AddValidatedPackageCommand, AddDeliveriesResponse>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly BranchCoordinatesOptions _branchCoordinates;
    private readonly ILogger<AddValidatedPackageHandler> _logger;

    public AddValidatedPackageHandler(
        IRouteRepository routeRepository,
        IUnitOfWork unitOfWork,
        IOptions<BranchCoordinatesOptions> branchCoordinates,
        ILogger<AddValidatedPackageHandler> logger)
    {
        _routeRepository = routeRepository;
        _unitOfWork = unitOfWork;
        _branchCoordinates = branchCoordinates.Value;
        _logger = logger;
    }

    public async Task<AddDeliveriesResponse> Handle(
        AddValidatedPackageCommand request,
        CancellationToken cancellationToken)
    {
        var route = await _routeRepository.GetLatestRouteForDateAsync(request.DeliveryDate, cancellationToken);

        if (route is null)
        {
            throw new DomainException(
                Error.NotFound(
                    "Route.NoRouteForDate",
                    "No route found for {date}. Create a route first.",
                    request.DeliveryDate.ToString("yyyy-MM-dd")));
        }

        var coordinate = ResolveCoordinate(request);
        var address = new DeliveryAddress(request.Address, coordinate);
        var package = new ValidatedPackage(
            request.PackageId, request.PatientId, request.PatientName, request.LabelData, request.DeliveryDayId,
            request.PatientPhone);

        route.AddDelivery(package, address);

        await _unitOfWork.CommitAsync(cancellationToken);

        var delivery = route.Deliveries[^1];

        return new AddDeliveriesResponse(
            route.Id,
            [new DeliveryItemResponse(delivery.Package.PatientId, delivery.Id, delivery.Status.ToString().ToLowerInvariant())]);
    }

    /// <summary>
    /// MS3 sends no coordinates when it could not match the label's address to a delivery calendar.
    /// The package still has to reach a route, so it falls back to the branch: sequencing then
    /// simply places it next to the starting point.
    /// </summary>
    private Coordinate ResolveCoordinate(AddValidatedPackageCommand request)
    {
        if (request.Latitude is { } latitude && request.Longitude is { } longitude)
            return new Coordinate(latitude, longitude);

        _logger.LogWarning(
            "Package {PackageId} arrived without coordinates; using the branch coordinates instead.",
            request.PackageId);

        return new Coordinate(_branchCoordinates.Latitude, _branchCoordinates.Longitude);
    }
}
