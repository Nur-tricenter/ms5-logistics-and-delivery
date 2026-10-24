namespace Nurtricenter.Infrastructure.ServiceDiscovery;

using Consul;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Registers THIS instance in Consul on start and deregisters it on stop, so the API Gateway can
/// discover and route to it without a static address list.
/// </summary>
/// <remarks>
/// <para>
/// <b>The invariant that matters: this class never throws.</b> A discovery outage must not take
/// the service down. If Consul is unreachable — or this deployment simply has no
/// <c>ServiceRegistration</c> configuration at all — the service logs a warning and keeps
/// serving traffic on the address its operator already knows.
/// </para>
/// <para>
/// Registration is retried with exponential backoff rather than attempted once. Consul agents
/// restart independently of the services registered with them, so the common failure is "the
/// agent was not up yet", which is exactly what a short retry loop absorbs without any human
/// noticing.
/// </para>
/// </remarks>
public sealed class ServiceRegistration : IHostedService
{
    /// <summary>
    /// How long Consul waits with a failing instance before evicting it from the catalog.
    /// </summary>
    public static readonly TimeSpan DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30);

    /// <summary>How often Consul probes the instance's own <c>/health</c>.</summary>
    public static readonly TimeSpan HealthCheckInterval = TimeSpan.FromSeconds(10);

    /// <summary>How long a single probe may take before it counts as a failure.</summary>
    public static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Path the check probes. It is the endpoint this same class registers with Consul, so the
    /// advertised instance is verified through the exact same contract a gateway would use.
    /// </summary>
    public const string HealthCheckPath = "/health";

    private const int MaxAttempts = 3;
    private static readonly TimeSpan RetryBaseDelay = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Host advertised when configuration does not pin one. A blank host would produce a
    /// registration the gateway cannot route to, which is worse than a local-only default.
    /// </summary>
    private const string DefaultHost = "localhost";

    private readonly IConsulClient _consulClient;
    private readonly ServiceRegistrationOptions _options;
    private readonly IServerPortResolver _portResolver;
    private readonly ILogger<ServiceRegistration> _logger;
    private readonly IHostApplicationLifetime? _lifetime;
    private volatile bool _registered;

    public ServiceRegistration(
        IConsulClient consulClient,
        IOptions<ServiceRegistrationOptions> options,
        IServerPortResolver portResolver,
        ILogger<ServiceRegistration> logger,
        IHostApplicationLifetime? lifetime = null)
    {
        _consulClient = consulClient;
        _options = options.Value;
        _portResolver = portResolver;
        _logger = logger;
        _lifetime = lifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.IsEnabled)
        {
            _logger.LogInformation(
                "No '{Section}:{Key}' is configured, so this instance is not registering itself in Consul.",
                ServiceRegistrationOptions.SectionName,
                nameof(ServiceRegistrationOptions.ServiceDiscoveryAddress));
            return;
        }

        if (_options.Port > 0 || _lifetime is null)
        {
            await RegisterAsync(cancellationToken);
            return;
        }

        // No configured port, so the advertised port is the one Kestrel binds. Under minimal
        // hosting (WebApplication) hosted services start BEFORE the server listens, so the bound
        // address is still empty here. Registration is therefore deferred until the application
        // has fully started and IServerAddressesFeature is populated.
        var lifetime = _lifetime;
        lifetime.ApplicationStarted.Register(() => _ = RegisterAsync(lifetime.ApplicationStopping));
    }

    private async Task RegisterAsync(CancellationToken cancellationToken)
    {
        var host = string.IsNullOrWhiteSpace(_options.Host) ? DefaultHost : _options.Host.Trim();
        if (!TryResolvePort(out var port))
        {
            _logger.LogWarning(
                "Could not resolve the port to advertise for {ServiceName}: neither '{Section}:Port' " +
                "is set nor could the bound port be read. This instance will not register itself in Consul.",
                _options.ServiceName,
                ServiceRegistrationOptions.SectionName);
            return;
        }

        var registration = BuildRegistration(host, port);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await _consulClient.Agent.ServiceRegister(registration, cancellationToken);
                _registered = true;
                _logger.LogInformation(
                    "Registered {ServiceId} ({ServiceName}) in Consul at {Address} with an HTTP check on {HealthUrl}.",
                    registration.ID,
                    registration.Name,
                    registration.Address,
                    registration.Check.HTTP);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Shutdown won the race with the registration; there is nothing left to report.
                return;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Attempt {Attempt} of {MaxAttempts} to register {ServiceId} in Consul failed.",
                    attempt,
                    MaxAttempts,
                    registration.ID);
            }

            if (attempt < MaxAttempts)
            {
                // Exponential backoff: 500ms, then 1s. Bounded on purpose — startup must not
                // block on a Consul that is not coming back.
                var delay = TimeSpan.FromMilliseconds(RetryBaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        _logger.LogWarning(
            "Could not register {ServiceId} in {DiscoveryAddress} after {MaxAttempts} attempts. The service " +
            "keeps running and is reachable directly; only gateway discovery is degraded.",
            registration.ID,
            _options.ServiceDiscoveryAddress,
            MaxAttempts);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_registered)
        {
            return;
        }

        try
        {
            await _consulClient.Agent.ServiceDeregister(_options.ResolveServiceId(), cancellationToken);
            _registered = false;
            _logger.LogInformation("Deregistered {ServiceId} from Consul.", _options.ResolveServiceId());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Shutdown cancelled the deregistration of {ServiceId}; Consul will evict it after {Eviction}.",
                _options.ResolveServiceId(),
                DeregisterCriticalServiceAfter);
        }
        catch (Exception exception)
        {
            // Deregistration is an optimisation, not a requirement: an instance killed without
            // deregistering is still reaped by DeregisterCriticalServiceAfter.
            _logger.LogWarning(
                exception,
                "Could not deregister {ServiceId} from Consul; it will be evicted after {Eviction}.",
                _options.ResolveServiceId(),
                DeregisterCriticalServiceAfter);
        }
    }

    private AgentServiceRegistration BuildRegistration(string host, int port) => new()
    {
        ID = _options.ResolveServiceId(),
        Name = _options.ServiceName,
        Address = host,
        Port = port,
        Check = new AgentServiceCheck
        {
            // Probe the instance through its own public endpoint rather than a bespoke
            // Consul-only path: whatever /health reports is exactly what a gateway would see.
            HTTP = $"http://{host}:{port}{HealthCheckPath}",
            Interval = HealthCheckInterval,
            Timeout = HealthCheckTimeout,
            DeregisterCriticalServiceAfter = DeregisterCriticalServiceAfter
        }
    };

    /// <summary>
    /// Prefers the CONFIGURED port and only falls back to the port the server actually bound to.
    /// </summary>
    /// <remarks>
    /// The configured port is the one the gateway was told about; the bound port exists purely as
    /// a convenience for a local run where nobody pinned one. Reading the bound port FIRST would
    /// be the bug: during <c>StartAsync</c> it is frequently still empty, and the fallback would
    /// silently never fire.
    /// </remarks>
    private bool TryResolvePort(out int port)
    {
        if (_options.Port > 0)
        {
            port = _options.Port;
            return true;
        }

        var bound = _portResolver.ResolveBoundPort();
        if (bound is > 0)
        {
            port = bound.Value;
            return true;
        }

        port = 0;
        return false;
    }
}
