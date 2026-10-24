namespace Nurtricenter.WorkerService;

using Joseco.Communication.External.RabbitMQ.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nurtricenter.Contracts.Integration;
using RabbitMQ.Client;

/// <summary>
/// Declares this service's ONE queue — <c>nurtricenter.default</c> — and binds it to every
/// exchange in <see cref="NurtricenterExchanges.All"/>. It exists to be the COMPLETE RECORD of
/// everything the service publishes, and for that reason it is deliberately NEVER CONSUMED.
/// </summary>
/// <remarks>
/// <para>
/// <b>Do NOT "improve" this into <c>AddRabbitMqConsumer</c>.</b> That extension declares the
/// queue, binds it to ONE exchange AND starts a consumer. Registering it once per contract on
/// the same queue name would put several COMPETING consumers on one queue: RabbitMQ delivers each
/// message to exactly one of them, so a <c>CourierCreated</c> could be handed to the
/// <c>DeliveryFailed</c> consumer, fail to deserialize, and be dead-lettered. Per-consumer queues
/// are created later by each consuming microservice against these exchanges; this queue stays a
/// passive sink.
/// </para>
/// <para>
/// Because the queue is durable and never consumed, its depth IS the number of events ever
/// published, and its contents are the full history.
/// </para>
/// <para>
/// The exchange flags are NOT a free choice: they MUST match what
/// <c>Joseco.Communication.External.RabbitMQ 1.1.0</c> declares on the publish side, because
/// RabbitMQ rejects a redeclaration with different arguments (<c>406 PRECONDITION_FAILED</c>).
/// The library declares its exchanges as <c>fanout</c> + <c>durable: false</c>.
/// </para>
/// <para>
/// The QUEUE, on the other hand, is only ever declared here, so it is durable: the record must
/// outlive the worker process.
/// </para>
/// <para>
/// The connection is opened INSIDE <see cref="ExecuteAsync"/> and retried, not in the
/// constructor: a broker that is briefly down must not turn into a failed host start.
/// </para>
/// </remarks>
public sealed class DefaultQueueRegistrar : BackgroundService
{
    /// <summary>
    /// Host port used when the configuration does not pin one. Mirrors the hard requirement of
    /// <c>AddNurtricenterRabbitMq</c>: Joseco.Communication.External.RabbitMQ 1.1.0 builds its own
    /// <c>ConnectionFactory</c> from HostName/UserName/Password only and silently ignores
    /// <c>RabbitMqSettings.Port</c>, so the broker must be reachable on 5672.
    /// </summary>
    private const int DefaultPort = 5672;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Exchanges are declared NON-durable on purpose, matching what
    /// <c>Joseco.Communication.External.RabbitMQ 1.1.0</c> declares at publish time.
    /// </summary>
    private const bool ExchangesAreDurable = false;

    /// <summary>Exchanges are never auto-deleted once declared.</summary>
    private const bool ExchangesAutoDelete = false;

    /// <summary>The queue exists to outlive the worker process, so it is durable.</summary>
    private const bool QueueIsDurable = true;

    /// <summary>
    /// No queue/exchange arguments are needed. Passed as an empty map rather than <c>null</c>
    /// because RabbitMQ.Client 7.x declares the parameter as non-nullable.
    /// </summary>
    private static readonly IDictionary<string, object?> NoArguments = new Dictionary<string, object?>();

    private readonly RabbitMqSettings _settings;
    private readonly ILogger<DefaultQueueRegistrar> _logger;

    public DefaultQueueRegistrar(RabbitMqSettings settings, ILogger<DefaultQueueRegistrar> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <summary>The single queue this service declares and binds.</summary>
    public const string QueueName = NurtricenterExchanges.DefaultQueue;

    /// <summary>
    /// The exchanges <see cref="QueueName"/> is bound to. Exposed so tests can assert the
    /// registrar covers every exchange without reaching into a broker.
    /// </summary>
    public static IReadOnlyList<string> BoundExchanges => NurtricenterExchanges.All;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DeclareTopologyAsync(stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Could not declare queue {Queue} bound to {ExchangeCount} exchange(s). Retrying in {RetryDelay}.",
                    QueueName,
                    BoundExchanges.Count,
                    RetryDelay);
            }

            try
            {
                await Task.Delay(RetryDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Opens one connection, declares the queue and every exchange, then binds. Idempotent: the
    /// broker treats a redeclaration with identical arguments as a no-op, so this is safe on
    /// every worker restart and across concurrently running workers.
    /// </summary>
    private async Task DeclareTopologyAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host)
            || string.IsNullOrWhiteSpace(_settings.UserName)
            || string.IsNullOrWhiteSpace(_settings.Password)
            || string.IsNullOrWhiteSpace(_settings.VirtualHost))
        {
            throw new InvalidOperationException(
                "RabbitMqSettings must define Host, UserName, Password and VirtualHost before the " +
                $"'{QueueName}' queue can be declared.");
        }

        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port > 0 ? _settings.Port : DefaultPort,
            UserName = _settings.UserName,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost
        };

        await using var connection = await factory.CreateConnectionAsync("nurtricenter-default-queue", cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        // Durable, non-exclusive, never auto-deleted: the record must outlive this worker.
        await channel.QueueDeclareAsync(
            QueueName,
            durable: QueueIsDurable,
            exclusive: false,
            autoDelete: false,
            arguments: NoArguments,
            cancellationToken: cancellationToken);

        foreach (var exchange in BoundExchanges)
        {
            // fanout + non-durable mirror the publisher side exactly: any mismatch here is a
            // 406 PRECONDITION_FAILED on the next publish.
            await channel.ExchangeDeclareAsync(
                exchange,
                ExchangeType.Fanout,
                durable: ExchangesAreDurable,
                autoDelete: ExchangesAutoDelete,
                arguments: NoArguments,
                cancellationToken: cancellationToken);

            // Empty routing key: a fanout ignores it, but RabbitMQ requires it to be non-null.
            await channel.QueueBindAsync(
                QueueName,
                exchange,
                routingKey: string.Empty,
                arguments: NoArguments,
                cancellationToken: cancellationToken);
        }

        // No BasicConsume/BasicGet here, and no consumer ever will be. Acking a message removes
        // it, and this queue's whole purpose is to keep every published message.
        _logger.LogInformation(
            "Queue {Queue} is declared and bound to {ExchangeCount} exchange(s): {Exchanges}.",
            QueueName,
            BoundExchanges.Count,
            string.Join(", ", BoundExchanges));
    }
}
