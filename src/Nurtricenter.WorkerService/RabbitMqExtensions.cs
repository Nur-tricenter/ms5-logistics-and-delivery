namespace Nurtricenter.WorkerService;

using Joseco.Communication.External.Contracts.Message;
using Joseco.Communication.External.Contracts.Services;
using Joseco.Communication.External.RabbitMQ;
using Joseco.Communication.External.RabbitMQ.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nur.Tricenter.Integration.Messages.Contracting;
using Nurtricenter.Infrastructure.Messaging;

public static class RabbitMqExtensions
{
    /// <summary>
    /// Queue MS5 consumes <c>PackageValidated</c> from (MS3, <c>package-validated</c> exchange).
    /// </summary>
    /// <remarks>
    /// The queue and its fanout binding are NOT declared by this service: nur-tricenter-local's
    /// <c>topology/rabbitmq-definitions.json</c> owns them (<c>{consumer}.{exchange}</c> naming).
    /// </remarks>
    public const string PackageValidatedQueue = "ms5.package-validated";

    /// <summary>
    /// Binds <see cref="RabbitMqSettings"/> from the <c>RabbitMqSettings</c> configuration
    /// section and registers the RabbitMQ publisher infrastructure.
    /// </summary>
    /// <remarks>
    /// Producer side only. The consumers are registered separately by
    /// <see cref="AddNurtricenterConsumers"/>, which must run after this method.
    /// <para>
    /// The settings instance MUST be registered before <c>AddRabbitMQ</c>: the library uses
    /// <c>TryAddSingleton</c>, so a later registration is silently ignored.
    /// </para>
    /// <para>
    /// NOTE: <see cref="RabbitMqSettings.Port"/> and <see cref="RabbitMqSettings.UseSsl"/> are
    /// silently ignored by Joseco.Communication.External.RabbitMQ 1.1.0 — both the publisher and
    /// the consumer build their own <c>ConnectionFactory</c> from HostName/UserName/Password
    /// only. The broker therefore MUST be reachable on host port 5672.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddNurtricenterRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration
            .GetSection("RabbitMqSettings")
            .Get<RabbitMqSettings>();

        if (settings is null)
        {
            throw new InvalidOperationException(
                "Configuration section 'RabbitMqSettings' is missing or empty.");
        }

        // Must precede AddRabbitMQ (TryAddSingleton).
        services.AddSingleton(settings);

        return services.AddRabbitMQ(settings);
    }

    /// <summary>
    /// Registers the inbound contracts this service consumes, each one as
    /// <c>Tracing → Idempotent(inbox) → consumer</c>.
    /// </summary>
    /// <remarks>
    /// <c>AddRabbitMqConsumer</c> starts a hosted service that connects to the broker at startup, so
    /// RabbitMQ becomes a boot dependency of the worker (the platform compose already waits for it to
    /// be healthy). Call this after <see cref="AddNurtricenterRabbitMq"/>, which registers the settings
    /// the consumer reads.
    /// </remarks>
    public static IServiceCollection AddNurtricenterConsumers(this IServiceCollection services)
    {
        services.AddConsumer<PackageValidated, PackageValidatedConsumer>(PackageValidatedQueue);

        return services;
    }

    private static void AddConsumer<T, TConsumer>(this IServiceCollection services, string queue)
        where T : IntegrationMessage
        where TConsumer : class, IIntegrationMessageConsumer<T>
    {
        services.AddTransient<TConsumer>();
        services.AddTransient<IdempotentConsumer<T, TConsumer>>();
        services.AddTransient<TracingConsumer<T, IdempotentConsumer<T, TConsumer>>>();
        services.AddRabbitMqConsumer<T, TracingConsumer<T, IdempotentConsumer<T, TConsumer>>>(queue);
    }
}
