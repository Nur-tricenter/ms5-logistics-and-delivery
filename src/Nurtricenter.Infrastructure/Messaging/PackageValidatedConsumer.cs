namespace Nurtricenter.Infrastructure.Messaging;

using Joseco.Communication.External.Contracts.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Nur.Tricenter.Integration.Messages.Contracting;
using Nurtricenter.Application.Commands.Deliveries.AddValidatedPackage;

/// <summary>
/// <c>ms5.package-validated</c> → adds the package MS3's quality control just released to today's
/// route. This is the event-driven replacement for <c>POST /api/v1/packages/deliveries</c>, which
/// stays as a manual fallback.
/// </summary>
/// <remarks>
/// A missing route for today throws: the message is retried and, once retries are exhausted, parked in
/// the queue's error queue, so an operator can create the route and requeue it.
/// </remarks>
public sealed class PackageValidatedConsumer(
    IMediator mediator,
    ILogger<PackageValidatedConsumer> logger) : IIntegrationMessageConsumer<PackageValidated>
{
    public async Task HandleAsync(PackageValidated message, CancellationToken cancellationToken = default)
    {
        var packageId = message.PackageId.ToString();
        var labelData = message.TrackingNumber;

        if (string.IsNullOrWhiteSpace(labelData))
        {
            logger.LogWarning("Package {PackageId} has no tracking number; using its id as label data.", packageId);
            labelData = packageId;
        }

        var command = new AddValidatedPackageCommand(
            packageId,
            message.PatientId.ToString(),
            message.DeliveryAddress,
            message.Latitude,
            message.Longitude,
            labelData);

        var response = await mediator.Send(command, cancellationToken);

        logger.LogInformation(
            "Package {PackageId} (production order {ProductionOrderId}) added to route {RouteId}.",
            packageId, message.ProductionOrderId, response.RouteId);
    }
}
