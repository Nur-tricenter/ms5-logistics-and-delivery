namespace Nurtricenter.Infrastructure.Observability;

using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;

/// <summary>
/// Stamps the ambient distributed trace onto every outbox message before it is staged, so the
/// correlation identifier survives the jump from the WebApi process to the WorkerService process.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> <see cref="Joseco.Outbox.Contracts.Model.OutboxMessage{T}"/>'s single
/// argument constructor leaves <c>CorrelationId</c>, <c>TraceId</c> and <c>SpanId</c> null, and it
/// does not read <see cref="System.Diagnostics.Activity.Current"/> either.
/// </para>
/// <para>
/// <b>Why the values come from <see cref="System.Diagnostics.Activity.Current"/>.</b> Because the
/// correlation identifier IS the trace identifier: one identifier then ties the HTTP request, the
/// database row, the worker's log line and the published contract together. The domain is
/// deliberately not involved: aggregates, domain events and handlers must not know that tracing
/// exists, so the read happens here, in the infrastructure layer, at the one place that writes the
/// row.
/// </para>
/// <para>
/// <b>Known limit, not a bug.</b> <c>IExternalPublisher.PublishAsync</c> accepts no headers, so
/// trace context can only travel as a JSON property inside <c>IntegrationMessage.CorrelationId</c>.
/// </para>
/// </remarks>
/// <remarks>
/// Implements <see cref="IOutboxService{TEvent}"/> only. Reading rows back is
/// <see cref="IOutboxRepository{TEvent}"/>, which is a separate interface resolved on the same
/// <c>EfCoreOutboxService</c> instance and must not be stamped: a row the worker reads was stamped
/// when it was written, and re-stamping it here would overwrite the originating request's trace with
/// the worker's poll activity.
/// </remarks>
internal sealed class OutboxTracingService<TEvent> : IOutboxService<TEvent>
{
    private readonly IOutboxService<TEvent> _inner;

    public OutboxTracingService(IOutboxService<TEvent> inner)
    {
        _inner = inner;
    }

    public async Task AddAsync(OutboxMessage<TEvent> message)
    {
        Stamp(message);
        await _inner.AddAsync(message);
    }

    private static void Stamp(OutboxMessage<TEvent> message)
    {
        var activity = System.Diagnostics.Activity.Current;
        if (activity is null)
        {
            return;
        }

        message.TraceId = activity.TraceId.ToString();
        message.SpanId = activity.SpanId.ToString();

        // TraceId doubles as the correlation id: one identifier across request, row and message.
        message.CorrelationId ??= activity.TraceId.ToString();
    }
}
