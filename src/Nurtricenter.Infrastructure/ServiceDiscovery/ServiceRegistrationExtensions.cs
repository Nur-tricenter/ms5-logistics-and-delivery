namespace Nurtricenter.Infrastructure.ServiceDiscovery;

using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Answers whether the Consul-dependent surface should be wired up at all.
    /// </summary>
    /// <remarks>
    /// Deliberately a single method so the hosted service and the PostgreSQL health check are
    /// gated by the SAME predicate and can never drift apart.
    /// </remarks>
    public static bool IsServiceDiscoveryEnabled(this IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(
            configuration[$"{ServiceRegistrationOptions.SectionName}:{nameof(ServiceRegistrationOptions.ServiceDiscoveryAddress)}"]);

    /// <summary>
    /// Registers the Consul self-registration hosted service — but ONLY when
    /// <c>ServiceRegistration:ServiceDiscoveryAddress</c> is present.
    /// </summary>
    /// <remarks>
    /// It is an opt-in extension rather than part of <c>AddInfrastructure</c> on purpose.
    /// <c>Nurtricenter.WorkerService</c> calls <c>AddInfrastructure</c> too, and it is a console
    /// host that publishes to RabbitMQ: it has no HTTP endpoint, so there is nothing for a gateway
    /// to route to and nothing for Consul to health-check. Folding registration into
    /// <c>AddInfrastructure</c> would make "which hosts call this?" the only thing keeping a
    /// phantom service out of the registry; here the answer is instead explicit at every call
    /// site, and the WorkerService simply never calls it.
    /// </remarks>
    public static IServiceCollection AddConsulServiceRegistration(this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.IsServiceDiscoveryEnabled())
        {
            return services;
        }

        // The client is created against the CONFIGURED agent address rather than
        // Consul's default of http://127.0.0.1:8500. A malformed address throws here, at
        // composition time, where it is still a configuration mistake rather than a mystery.
        var address = new Uri(
            configuration[$"{ServiceRegistrationOptions.SectionName}:{nameof(ServiceRegistrationOptions.ServiceDiscoveryAddress)}"]!.Trim());

        services.Configure<ServiceRegistrationOptions>(configuration.GetSection(ServiceRegistrationOptions.SectionName));

        services.TryAddSingleton<IServerPortResolver, UnknownServerPortResolver>();

        services.AddSingleton<IConsulClient>(_ => new ConsulClient(cfg => cfg.Address = address));

        services.AddSingleton<ServiceRegistration>();
        services.AddHostedService(sp => sp.GetRequiredService<ServiceRegistration>());

        return services;
    }
}

/// <summary>
/// Fallback used when no host supplied a real bound port (the console worker, or a web host that
/// forgot to register its own resolver). It reports "unknown" so registration is skipped with a
/// warning instead of being advertised on port 0.
/// </summary>
internal sealed class UnknownServerPortResolver : IServerPortResolver
{
    public int? ResolveBoundPort() => null;
}
