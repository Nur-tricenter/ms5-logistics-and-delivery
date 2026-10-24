namespace Nurtricenter.WorkerService;

using Joseco.Communication.External.RabbitMQ;
using Joseco.Communication.External.RabbitMQ.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class RabbitMqExtensions
{
    /// <summary>
    /// Binds <see cref="RabbitMqSettings"/> from the <c>RabbitMqSettings</c> configuration
    /// section and registers the RabbitMQ publisher infrastructure.
    /// </summary>
    /// <remarks>
    /// This service is a PURE PRODUCER: no consumer is registered, deliberately.
    /// <c>AddRabbitMqConsumer&lt;TMessage, THandler&gt;</c> spins up a hosted service that
    /// connects to the broker at startup and subscribes to a queue. There is no inbound
    /// contract to consume, so registering one would turn RabbitMQ into a hard boot
    /// dependency for a queue that does not exist.
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
}
