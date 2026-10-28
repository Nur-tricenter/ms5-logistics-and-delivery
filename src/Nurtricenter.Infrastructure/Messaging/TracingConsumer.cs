namespace Nurtricenter.Infrastructure.Messaging;

using System.Diagnostics;
using Joseco.Communication.External.Contracts.Message;
using Joseco.Communication.External.Contracts.Services;

public static class MessagingTelemetry
{
    /// <summary>
    /// ActivitySource for consumer spans. The WorkerService listens to it so <see cref="Activity.Current"/>
    /// exists while a message is handled, which is what <c>OutboxTracingService</c> stamps onto the outbox
    /// rows the handler stages.
    /// </summary>
    public const string ActivitySourceName = "Nurtricenter.Messaging";

    internal static readonly ActivitySource Source = new(ActivitySourceName);
}

/// <summary>
/// Opens a <c>rabbitmq.consume</c> span per message, parented to the publisher's trace when the
/// message's <c>CorrelationId</c> carries one (same convention as the other Nur-Tricenter services).
/// </summary>
public sealed class TracingConsumer<T, TInner>(TInner inner) : IIntegrationMessageConsumer<T>
    where T : IntegrationMessage
    where TInner : class, IIntegrationMessageConsumer<T>
{
    public async Task HandleAsync(T message, CancellationToken cancellationToken = default)
    {
        ActivityContext.TryParse(ToTraceParent(message.CorrelationId), null, out var parent);

        using var activity = MessagingTelemetry.Source.StartActivity("rabbitmq.consume", ActivityKind.Consumer, parent);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation.name", "consume");
        activity?.SetTag("messaging.destination.name", typeof(T).Name);
        activity?.SetTag("messaging.message.id", message.Id);
        activity?.SetTag("messaging.correlation_id", message.CorrelationId);
        activity?.SetTag("messaging.source", message.Source);

        try
        {
            await inner.HandleAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }

    // A W3C traceparent as-is, or a bare 32-hex trace id promoted to one.
    private static string? ToTraceParent(string? correlationId)
    {
        var candidate = correlationId?.Trim();
        if (string.IsNullOrEmpty(candidate))
            return null;

        if (candidate.StartsWith("00-", StringComparison.Ordinal))
            return candidate;

        return candidate.Length == 32 && candidate.All(Uri.IsHexDigit)
            ? $"00-{candidate}-{candidate[..16]}-01"
            : null;
    }
}
