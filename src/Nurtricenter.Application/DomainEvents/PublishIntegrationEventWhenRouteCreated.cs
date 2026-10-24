namespace Nurtricenter.Application.DomainEvents;

using Joseco.Communication.External.Contracts.Services;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.Extensions.Logging;
using Nur.Tricenter.Integration.Messages.Logistics;
using Nurtricenter.Contracts.Integration;
using Nurtricenter.Core.Domain.Route.Events;

/// <summary>
/// Translates the outbox message that carries <see cref="RouteCreatedEvent"/> into the
/// <see cref="RouteCreated"/> external contract and publishes it to RabbitMQ.
/// </summary>
public sealed class PublishIntegrationEventWhenRouteCreated : INotificationHandler<OutboxMessage<RouteCreatedEvent>>
{
    public const string Destination = NurtricenterExchanges.RouteCreated;
    public const string Source = "nurtricenter";

    private readonly ILogger<PublishIntegrationEventWhenRouteCreated> _logger;
    private readonly IExternalPublisher _publisher;

    public PublishIntegrationEventWhenRouteCreated(
        ILogger<PublishIntegrationEventWhenRouteCreated> logger,
        IExternalPublisher publisher)
    {
        _logger = logger;
        _publisher = publisher;
    }

    public async Task Handle(OutboxMessage<RouteCreatedEvent> notification, CancellationToken cancellationToken = default)
    {
        var routeCreated = notification.Content;

        var message = new RouteCreated(OutboxCorrelation.Resolve(notification.CorrelationId), Source)
        {
            RouteId = routeCreated.RouteId,
            CourierId = routeCreated.CourierId,
            ScheduledDate = routeCreated.ScheduledDate
        };

        _logger.LogInformation(
            "Publishing {EventType} for route {RouteId} on {ScheduledDate}",
            message.EventType, message.RouteId, message.ScheduledDate);

        await _publisher.PublishAsync(message, Destination, declareDestination: true);
    }
}
