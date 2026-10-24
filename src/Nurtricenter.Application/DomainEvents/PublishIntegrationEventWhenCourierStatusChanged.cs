namespace Nurtricenter.Application.DomainEvents;

using Joseco.Communication.External.Contracts.Services;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.Extensions.Logging;
using Nur.Tricenter.Integration.Messages.Logistics;
using Nurtricenter.Contracts.Integration;
using Nurtricenter.Core.Domain.Courier.Events;

/// <summary>
/// Translates the outbox message that carries <see cref="CourierStatusChangedEvent"/> into the
/// <see cref="CourierStatusChanged"/> external contract and publishes it to RabbitMQ.
/// </summary>
public sealed class PublishIntegrationEventWhenCourierStatusChanged : INotificationHandler<OutboxMessage<CourierStatusChangedEvent>>
{
    public const string Destination = NurtricenterExchanges.CourierStatusChanged;
    public const string Source = "nurtricenter";

    private readonly ILogger<PublishIntegrationEventWhenCourierStatusChanged> _logger;
    private readonly IExternalPublisher _publisher;

    public PublishIntegrationEventWhenCourierStatusChanged(
        ILogger<PublishIntegrationEventWhenCourierStatusChanged> logger,
        IExternalPublisher publisher)
    {
        _logger = logger;
        _publisher = publisher;
    }

    public async Task Handle(OutboxMessage<CourierStatusChangedEvent> notification, CancellationToken cancellationToken = default)
    {
        var statusChanged = notification.Content;

        var message = new CourierStatusChanged(OutboxCorrelation.Resolve(notification.CorrelationId), Source)
        {
            CourierId = statusChanged.CourierId,
            OldStatus = statusChanged.OldStatus.ToString(),
            NewStatus = statusChanged.NewStatus.ToString()
        };

        _logger.LogInformation(
            "Publishing {EventType} for courier {CourierId}: {OldStatus} -> {NewStatus}",
            message.EventType, message.CourierId, message.OldStatus, message.NewStatus);

        await _publisher.PublishAsync(message, Destination, declareDestination: true);
    }
}
