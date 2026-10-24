namespace Nurtricenter.Application.DomainEvents;

using Joseco.Communication.External.Contracts.Services;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.Extensions.Logging;
using Nur.Tricenter.Integration.Messages.Logistics;
using Nurtricenter.Contracts.Integration;
using Nurtricenter.Core.Domain.Route.Events;

/// <summary>
/// Translates the outbox message that carries <see cref="CourierAssignedToRouteEvent"/> into the
/// <see cref="CourierAssignedToRoute"/> external contract and publishes it to RabbitMQ.
/// </summary>
public sealed class PublishIntegrationEventWhenCourierAssignedToRoute : INotificationHandler<OutboxMessage<CourierAssignedToRouteEvent>>
{
    public const string Destination = NurtricenterExchanges.CourierAssignedToRoute;
    public const string Source = "nurtricenter";

    private readonly ILogger<PublishIntegrationEventWhenCourierAssignedToRoute> _logger;
    private readonly IExternalPublisher _publisher;

    public PublishIntegrationEventWhenCourierAssignedToRoute(
        ILogger<PublishIntegrationEventWhenCourierAssignedToRoute> logger,
        IExternalPublisher publisher)
    {
        _logger = logger;
        _publisher = publisher;
    }

    public async Task Handle(OutboxMessage<CourierAssignedToRouteEvent> notification, CancellationToken cancellationToken = default)
    {
        var courierAssigned = notification.Content;

        var message = new CourierAssignedToRoute(OutboxCorrelation.Resolve(notification.CorrelationId), Source)
        {
            RouteId = courierAssigned.RouteId,
            CourierId = courierAssigned.CourierId
        };

        _logger.LogInformation(
            "Publishing {EventType} for route {RouteId} and courier {CourierId}",
            message.EventType, message.RouteId, message.CourierId);

        await _publisher.PublishAsync(message, Destination, declareDestination: true);
    }
}
