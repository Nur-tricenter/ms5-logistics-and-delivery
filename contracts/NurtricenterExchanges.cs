namespace Nurtricenter.Contracts.Integration;

/// <summary>
/// Single source of truth for the RabbitMQ topology the nurtricenter service owns: one fanout
/// exchange per published event, plus the single queue that accumulates the complete record of
/// everything published.
/// </summary>
/// <remarks>
/// Every publisher takes its <c>Destination</c> from a constant declared HERE, and the
/// <c>DefaultQueueRegistrar</c> in <c>Nurtricenter.WorkerService</c> binds
/// <see cref="DefaultQueue"/> to <see cref="All"/>. That way the registrar cannot bind a queue
/// to an exchange nobody publishes to, and a publisher cannot publish to an exchange nothing is
/// bound to (which on a fanout exchange means the message is silently dropped).
/// <para>
/// Adding an event means adding a constant here, a contract, a publisher, and a line in
/// <see cref="All"/>.
/// </para>
/// </remarks>
public static class NurtricenterExchanges
{
    /// <summary>Exchange for <c>CourierCreated</c>.</summary>
    public const string CourierCreated = "courier-created";

    /// <summary>Exchange for <c>CourierStatusChanged</c>.</summary>
    public const string CourierStatusChanged = "courier-status-changed";

    /// <summary>Exchange for <c>RouteCreated</c>.</summary>
    public const string RouteCreated = "route-created";

    /// <summary>Exchange for <c>CourierAssignedToRoute</c>.</summary>
    public const string CourierAssignedToRoute = "courier-assigned-to-route";

    /// <summary>Exchange for <c>DeliveryAddedToRoute</c>.</summary>
    public const string DeliveryAddedToRoute = "delivery-added-to-route";

    /// <summary>Exchange for <c>RouteStarted</c>.</summary>
    public const string RouteStarted = "route-started";

    /// <summary>Exchange for <c>RouteCompleted</c>.</summary>
    public const string RouteCompleted = "route-completed";

    /// <summary>Exchange for <c>RouteCancelled</c>.</summary>
    public const string RouteCancelled = "route-cancelled";

    /// <summary>Exchange for <c>DeliveryCompleted</c>.</summary>
    public const string DeliveryCompleted = "delivery-completed";

    /// <summary>Exchange for <c>DeliveryFailed</c>.</summary>
    public const string DeliveryFailed = "delivery-failed";

    /// <summary>
    /// The one queue this service declares. Its purpose is to be the COMPLETE RECORD of everything
    /// the service publishes: it is bound to every exchange in <see cref="All"/> and never
    /// consumed, so the whole event history accumulates in it.
    /// </summary>
    /// <remarks>
    /// Per-consumer queues (other microservices) are intentionally NOT declared here. They are
    /// created by each consuming microservice against these exchanges.
    /// </remarks>
    public const string DefaultQueue = "nurtricenter.default";

    private static readonly IReadOnlyList<string> _all =
    [
        CourierCreated,
        CourierStatusChanged,
        RouteCreated,
        CourierAssignedToRoute,
        DeliveryAddedToRoute,
        RouteStarted,
        RouteCompleted,
        RouteCancelled,
        DeliveryCompleted,
        DeliveryFailed
    ];

    /// <summary>
    /// Every exchange the service publishes to, in registry order. The registrar binds
    /// <see cref="DefaultQueue"/> to exactly these, so this list is the authoritative topology.
    /// </summary>
    public static IReadOnlyList<string> All => _all;

    /// <summary>
    /// <c>true</c> when <paramref name="exchange"/> is one of the exchanges in <see cref="All"/>.
    /// Used to keep publisher destinations and registrar bindings in step.
    /// </summary>
    public static bool Contains(string? exchange) =>
        exchange is not null && _all.Contains(exchange, StringComparer.Ordinal);
}
