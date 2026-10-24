namespace Nurtricenter.Application.DomainEvents;

using System.Diagnostics;

/// <summary>
/// Resolves the correlation id for a contract published from the outbox.
/// </summary>
/// <remarks>
/// Joseco's <c>OutboxProcessor</c> does not dispatch the stored row: it builds a NEW
/// <c>OutboxMessage&lt;TEvent&gt;</c> from the row's <c>Content</c> only, so the
/// <c>CorrelationId</c> stamped at write time arrives as <c>null</c>. It does, however, wrap each
/// dispatch in a "Joseco.Outbox" activity whose parent is the row's stored trace, and the row's
/// correlation id IS that trace id (see <c>OutboxTracingService</c>). The worker listens to that
/// activity source, so <see cref="Activity.Current"/> recovers the original id here.
/// </remarks>
public static class OutboxCorrelation
{
    public static string? Resolve(string? correlationId) =>
        correlationId ?? Activity.Current?.TraceId.ToString();
}
