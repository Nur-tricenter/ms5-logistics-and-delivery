using FluentValidation;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Nurtricenter.Application.DomainEvents;
using Nurtricenter.Core.Domain.Courier.Events;
using Nurtricenter.Core.Domain.Delivery.Events;
using Nurtricenter.Core.Domain.Route.Events;

namespace Nurtricenter.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly));

        services.AddValidatorsFromAssembly(
            typeof(AssemblyReference).Assembly,
            includeInternalTypes: true);

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationBehavior<,>));

        // NOTE: MediatR's assembly scan also picks up the outbox-publishing handlers, but they
        // are REMOVED here on purpose. They depend on IExternalPublisher, which is bound only by
        // Nurtricenter.WorkerService — the single host that runs OutboxProcessor<DomainEvent> and
        // therefore the single host that ever dispatches outbox messages. Leaving them registered
        // in the WebApi would make DI validation fail there for handlers that can never run.
        // Hosts that own the outbox processor must call AddIntegrationMessaging instead.
        RemoveOutboxPublishHandlers(services);

        return services;
    }

    /// <summary>
    /// Registers the outbox-to-broker publishing handlers. Call this ONLY from a host that
    /// runs the outbox processor and that has already registered an
    /// <c>Joseco.Communication.External.Contracts.Services.IExternalPublisher</c>.
    /// </summary>
    /// <remarks>
    /// Each handler is registered for the CONCRETE <c>OutboxMessage&lt;TEvent&gt;</c>, not for
    /// <c>OutboxMessage&lt;DomainEvent&gt;</c>: Joseco's <c>OutboxProcessor</c> re-wraps every row as
    /// <c>OutboxMessage&lt;{runtime event type}&gt;</c> before publishing it through MediatR, and
    /// generic classes are not variant, so a handler for the base type would never be invoked
    /// while the row is still marked as processed.
    /// </remarks>
    public static IServiceCollection AddIntegrationMessaging(this IServiceCollection services)
    {
        services.AddTransient<INotificationHandler<OutboxMessage<CourierCreatedEvent>>, PublishIntegrationEventWhenCourierCreated>();
        services.AddTransient<INotificationHandler<OutboxMessage<CourierStatusChangedEvent>>, PublishIntegrationEventWhenCourierStatusChanged>();
        services.AddTransient<INotificationHandler<OutboxMessage<RouteCreatedEvent>>, PublishIntegrationEventWhenRouteCreated>();
        services.AddTransient<INotificationHandler<OutboxMessage<CourierAssignedToRouteEvent>>, PublishIntegrationEventWhenCourierAssignedToRoute>();
        services.AddTransient<INotificationHandler<OutboxMessage<DeliveryAddedToRouteEvent>>, PublishIntegrationEventWhenDeliveryAddedToRoute>();
        services.AddTransient<INotificationHandler<OutboxMessage<RouteStartedEvent>>, PublishIntegrationEventWhenRouteStarted>();
        services.AddTransient<INotificationHandler<OutboxMessage<RouteCompletedEvent>>, PublishIntegrationEventWhenRouteCompleted>();
        services.AddTransient<INotificationHandler<OutboxMessage<RouteCancelledEvent>>, PublishIntegrationEventWhenRouteCancelled>();
        services.AddTransient<INotificationHandler<OutboxMessage<DeliveryCompletedEvent>>, PublishIntegrationEventWhenDeliveryCompleted>();
        services.AddTransient<INotificationHandler<OutboxMessage<DeliveryFailedEvent>>, PublishIntegrationEventWhenDeliveryFailed>();

        return services;
    }

    /// <summary>
    /// True for any <c>INotificationHandler&lt;OutboxMessage&lt;T&gt;&gt;</c> service type.
    /// </summary>
    public static bool IsOutboxPublishHandler(Type serviceType) =>
        serviceType.IsGenericType
        && serviceType.GetGenericTypeDefinition() == typeof(INotificationHandler<>)
        && serviceType.GetGenericArguments()[0] is { IsGenericType: true } notification
        && notification.GetGenericTypeDefinition() == typeof(OutboxMessage<>);

    private static void RemoveOutboxPublishHandlers(IServiceCollection services)
    {
        var descriptors = services.Where(d => IsOutboxPublishHandler(d.ServiceType)).ToList();
        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }
}
