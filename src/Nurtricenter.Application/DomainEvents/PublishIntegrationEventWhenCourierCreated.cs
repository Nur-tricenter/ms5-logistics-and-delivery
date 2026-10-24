namespace Nurtricenter.Application.DomainEvents;

using Joseco.Communication.External.Contracts.Services;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.Extensions.Logging;
using Nur.Tricenter.Integration.Messages.Logistics;
using Nurtricenter.Contracts.Integration;
using Nurtricenter.Core.Domain.Courier.Events;

/// <summary>
/// Translates the outbox message that carries <see cref="CourierCreatedEvent"/> into the
/// <see cref="CourierCreated"/> external contract and publishes it to RabbitMQ.
/// </summary>
public sealed class PublishIntegrationEventWhenCourierCreated : INotificationHandler<OutboxMessage<CourierCreatedEvent>>
{
    /// <summary>
    /// Exchange/routing destination for this contract. It MUST be passed explicitly: the
    /// library derives the default destination from <c>typeof(T)</c>, which for a runtime
    /// type that is not the compile-time generic argument silently routes to the wrong name.
    /// </summary>
    public const string Destination = NurtricenterExchanges.CourierCreated;

    /// <summary>Logical producer name stamped into every published contract.</summary>
    public const string Source = "nurtricenter";

    private readonly ILogger<PublishIntegrationEventWhenCourierCreated> _logger;
    private readonly IExternalPublisher _publisher;

    public PublishIntegrationEventWhenCourierCreated(
        ILogger<PublishIntegrationEventWhenCourierCreated> logger,
        IExternalPublisher publisher)
    {
        _logger = logger;
        _publisher = publisher;
    }

    public async Task Handle(OutboxMessage<CourierCreatedEvent> notification, CancellationToken cancellationToken = default)
    {
        var courierCreated = notification.Content;

        var message = new CourierCreated(OutboxCorrelation.Resolve(notification.CorrelationId), Source)
        {
            CourierId = courierCreated.CourierId,
            FullName = courierCreated.FullName,
            Status = courierCreated.Status.ToString()
        };

        _logger.LogInformation(
            "Publishing {EventType} for courier {CourierId}",
            message.EventType, message.CourierId);

        // declareDestination MUST be true: the publish is mandatory, so an undeclared
        // exchange makes the call fail with a channel error instead of silently dropping.
        await _publisher.PublishAsync(message, Destination, declareDestination: true);
    }
}
