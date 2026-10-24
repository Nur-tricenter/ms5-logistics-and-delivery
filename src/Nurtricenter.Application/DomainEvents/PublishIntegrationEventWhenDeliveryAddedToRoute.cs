namespace Nurtricenter.Application.DomainEvents;

using Joseco.Communication.External.Contracts.Services;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.Extensions.Logging;
using Nur.Tricenter.Integration.Messages.Logistics;
using Nurtricenter.Contracts.Integration;
using Nurtricenter.Core.Domain.Route.Events;

/// <summary>
/// Translates the outbox message that carries <see cref="DeliveryAddedToRouteEvent"/> into the
/// <see cref="DeliveryAddedToRoute"/> external contract and publishes it to RabbitMQ.
/// </summary>
public sealed class PublishIntegrationEventWhenDeliveryAddedToRoute : INotificationHandler<OutboxMessage<DeliveryAddedToRouteEvent>>
{
    public const string Destination = NurtricenterExchanges.DeliveryAddedToRoute;
    public const string Source = "nurtricenter";

    private readonly ILogger<PublishIntegrationEventWhenDeliveryAddedToRoute> _logger;
    private readonly IExternalPublisher _publisher;

    public PublishIntegrationEventWhenDeliveryAddedToRoute(
        ILogger<PublishIntegrationEventWhenDeliveryAddedToRoute> logger,
        IExternalPublisher publisher)
    {
        _logger = logger;
        _publisher = publisher;
    }

    public async Task Handle(OutboxMessage<DeliveryAddedToRouteEvent> notification, CancellationToken cancellationToken = default)
    {
        var deliveryAdded = notification.Content;

        var message = new DeliveryAddedToRoute(OutboxCorrelation.Resolve(notification.CorrelationId), Source)
        {
            RouteId = deliveryAdded.RouteId,
            DeliveryId = deliveryAdded.DeliveryId,
            PackageId = deliveryAdded.PackageId,
            PatientId = deliveryAdded.PatientId
        };

        _logger.LogInformation(
            "Publishing {EventType} for delivery {DeliveryId} on route {RouteId}",
            message.EventType, message.DeliveryId, message.RouteId);

        await _publisher.PublishAsync(message, Destination, declareDestination: true);
    }
}
