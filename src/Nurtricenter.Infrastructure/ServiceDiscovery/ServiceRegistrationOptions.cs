namespace Nurtricenter.Infrastructure.ServiceDiscovery;

/// <summary>
/// Configuration that drives this instance's <em>self</em>-registration in Consul.
/// </summary>
/// <remarks>
/// The key names are deliberately the ones the platform's shared compose files feed as
/// <c>ServiceRegistration__ServiceDiscoveryAddress</c>, <c>ServiceRegistration__Host</c> and
/// <c>ServiceRegistration__Port</c>. Renaming them here would make this service the one that
/// cannot be dropped into the same compose stack.
/// </remarks>
public class ServiceRegistrationOptions
{
    /// <summary>Configuration section these options are bound from.</summary>
    public const string SectionName = "ServiceRegistration";

    /// <summary>
    /// Address of the Consul agent, e.g. <c>http://localhost:8500</c>. This value is also the
    /// master switch for the whole feature: when it is absent or blank, nothing registers and
    /// nothing that requires a running Consul is wired up.
    /// </summary>
    public string? ServiceDiscoveryAddress { get; set; }

    /// <summary>
    /// Unique id of THIS instance. Defaults to <see cref="ServiceName"/>, which is correct for a
    /// single replica but collides as soon as a second replica starts, so a multi-replica
    /// deployment must set it explicitly.
    /// </summary>
    public string ServiceId { get; set; } = DefaultServiceId;

    /// <summary>
    /// Name the gateway routes by, i.e. the name every replica of this service shares.
    /// </summary>
    public string ServiceName { get; set; } = DefaultServiceId;

    /// <summary>
    /// Address the <em>gateway</em> uses to reach this instance. It is NOT necessarily an
    /// address this process can reach itself by: in Docker it is the compose service name, and
    /// that is exactly why it is configured rather than sniffed from the local socket table.
    /// </summary>
    public string? Host { get; set; }

    /// <summary>Port the <em>gateway</em> uses to reach this instance.</summary>
    public int Port { get; set; }

    /// <summary>
    /// Whether this instance should register itself at all.
    /// </summary>
    /// <remarks>
    /// This single predicate is the gate for the WHOLE Consul-dependent surface: the hosted
    /// service, the <c>IConsulClient</c> and the PostgreSQL health check are all wired behind
    /// it. That coupling is the point. If the PostgreSQL check were registered unconditionally,
    /// every test that boots the real host through <c>WebApplicationFactory</c> with the
    /// EF Core InMemory provider would open a TCP connection to a database that is not there,
    /// so <c>/health</c> would report Unhealthy in a suite that is testing routing and JSON
    /// shape. Tying both to one predicate means: no configuration, no external dependency, no
    /// surprises.
    /// </remarks>
    public bool IsEnabled => !string.IsNullOrWhiteSpace(ServiceDiscoveryAddress);

    internal const string DefaultServiceId = "nurtricenter-service";

    /// <summary>
    /// Returns <see cref="ServiceId"/> when it was set, otherwise <see cref="ServiceName"/>.
    /// Kept as a method rather than a plain property so an unset <c>ServiceId</c> tracks the
    /// configured name instead of freezing whatever the default happened to be.
    /// </summary>
    internal string ResolveServiceId() =>
        string.IsNullOrWhiteSpace(ServiceId) ? ServiceName.Trim() : ServiceId.Trim();
}
