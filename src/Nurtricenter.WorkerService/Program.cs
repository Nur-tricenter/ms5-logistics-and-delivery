namespace Nurtricenter.WorkerService;

using System.Diagnostics;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.EFCore;
using Nurtricenter.Application;
using Nurtricenter.Infrastructure;
using Serilog;
using Serilog.Enrichers.Span;

/// <summary>
/// Console host that turns the transactional outbox into published RabbitMQ events.
/// </summary>
/// <remarks>
/// A classic <c>Main</c> rather than top-level statements on purpose: top-level statements would
/// generate a second global <c>Program</c> type and make <c>WebApplicationFactory&lt;Program&gt;</c>
/// ambiguous in the test project, which references both this host and the web API.
/// </remarks>
public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Structured logging with the ambient trace stamped onto every event. Applied before the
        // service registrations so the bootstrap log lines are enriched too.
        builder.Services.AddSerilog((services, configuration) => configuration
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProcessId()
            .Enrich.WithProperty("ServiceName", "nurtricenter-service")
            .WriteTo.Console());

        // AddInfrastructure() does NOT call AddApplication() internally in this solution, so both
        // are called here, in that order.
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        // RabbitMQ producer side: binds RabbitMqSettings, registers it as a singleton and then the
        // publisher. No consumer is registered on purpose — this host has no inbound contract.
        builder.Services.AddNurtricenterRabbitMq(builder.Configuration);

        // The WorkerService is the only host that runs OutboxProcessor<DomainEvent>, so it is the
        // only host that needs the outbox-to-broker publishers. AddApplication() deliberately
        // leaves them out.
        builder.Services.AddIntegrationMessaging();

        builder.Services.AddOutboxBackgroundService<DomainEvent>();

        // Declares (and never consumes) the single `nurtricenter.default` queue, bound to every
        // exchange this service publishes to, so the broker keeps the complete record of
        // everything published.
        builder.Services.AddHostedService<DefaultQueueRegistrar>();

        // Joseco's OutboxProcessor re-creates each message from its content only, dropping the
        // stored CorrelationId, but it wraps every dispatch in a "Joseco.Outbox" activity parented
        // to the row's stored trace. ActivitySource only creates activities when someone listens,
        // so this listener is what makes Activity.Current carry the original trace id into the
        // publish handlers (see OutboxCorrelation).
        ActivitySource.AddActivityListener(new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Joseco.Outbox",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        });

        var host = builder.Build();
        await host.RunAsync();
    }
}
