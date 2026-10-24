namespace Nurtricenter.Application.DomainEvents;

using Joseco.Communication.External.Contracts.Services;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.Extensions.Logging;
using Nur.Tricenter.Integration.Messages.Logistics;
using Nurtricenter.Contracts.Integration;
using Nurtricenter.Core.Domain.Delivery.Events;

/// <summary>
/// Translates the outbox message that carries <see cref="DeliveryFailedEvent"/> into the
/// <see cref="DeliveryFailed"/> external contract and publishes it to RabbitMQ.
/// </summary>
public sealed class PublishIntegrationEventWhenDeliveryFailed : INotificationHandler<OutboxMessage<DeliveryFailedEvent>>
{
    public const string Destination = NurtricenterExchanges.DeliveryFailed;
    public const string Source = "nurtricenter";

    private readonly ILogger<PublishIntegrationEventWhenDeliveryFailed> _logger;
    private readonly IExternalPublisher _publisher;

    public PublishIntegrationEventWhenDeliveryFailed(
        ILogger<PublishIntegrationEventWhenDeliveryFailed> logger,
        IExternalPublisher publisher)
    {
        _logger = logger;
        _publisher = publisher;
    }

    public async Task Handle(OutboxMessage<DeliveryFailedEvent> notification, CancellationToken cancellationToken = default)
    {
        var deliveryFailed = notification.Content;

        var message = new DeliveryFailed(OutboxCorrelation.Resolve(notification.CorrelationId), Source)
        {
            DeliveryId = deliveryFailed.DeliveryId,
            RouteId = deliveryFailed.RouteId,
            Reason = deliveryFailed.Reason
        };

        _logger.LogInformation(
            "Publishing {EventType} for delivery {DeliveryId} on route {RouteId}",
            message.EventType, message.DeliveryId, message.RouteId);

        await _publisher.PublishAsync(message, Destination, declareDestination: true);
    }
}
