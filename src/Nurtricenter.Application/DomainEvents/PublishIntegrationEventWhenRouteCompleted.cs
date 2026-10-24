namespace Nurtricenter.Application.DomainEvents;

using Joseco.Communication.External.Contracts.Services;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.Extensions.Logging;
using Nur.Tricenter.Integration.Messages.Logistics;
using Nurtricenter.Contracts.Integration;
using Nurtricenter.Core.Domain.Route.Events;

/// <summary>
/// Translates the outbox message that carries <see cref="RouteCompletedEvent"/> into the
/// <see cref="RouteCompleted"/> external contract and publishes it to RabbitMQ.
/// </summary>
public sealed class PublishIntegrationEventWhenRouteCompleted : INotificationHandler<OutboxMessage<RouteCompletedEvent>>
{
    public const string Destination = NurtricenterExchanges.RouteCompleted;
    public const string Source = "nurtricenter";

    private readonly ILogger<PublishIntegrationEventWhenRouteCompleted> _logger;
    private readonly IExternalPublisher _publisher;

    public PublishIntegrationEventWhenRouteCompleted(
        ILogger<PublishIntegrationEventWhenRouteCompleted> logger,
        IExternalPublisher publisher)
    {
        _logger = logger;
        _publisher = publisher;
    }

    public async Task Handle(OutboxMessage<RouteCompletedEvent> notification, CancellationToken cancellationToken = default)
    {
        var routeCompleted = notification.Content;

        var message = new RouteCompleted(OutboxCorrelation.Resolve(notification.CorrelationId), Source)
        {
            RouteId = routeCompleted.RouteId
        };

        _logger.LogInformation(
            "Publishing {EventType} for route {RouteId}",
            message.EventType, message.RouteId);

        await _publisher.PublishAsync(message, Destination, declareDestination: true);
    }
}
