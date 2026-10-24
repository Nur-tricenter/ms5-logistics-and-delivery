using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Nurtricenter.Api.Configuration;
using Nurtricenter.Api.Utils;
using Nurtricenter.Infrastructure.ServiceDiscovery;
using Nurtricenter.Infrastructure.Health;
using Scalar.AspNetCore;
using Serilog;

namespace Nurtricenter.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();

        // FluentValidation — auto-validate requests in MVC pipeline
        services.AddFluentValidationAutoValidation();

        // DomainException → HTTP ProblemDetails mapping
        services.AddExceptionHandler<DomainExceptionHandler>();
        services.AddProblemDetails();

        services.AddOpenApi();

        services.AddAuthorization();

        // /health is always available and always mapped, because Consul probes it. It is wired
        // with ZERO checks by default: a host with no discovery configuration has nothing
        // external to report on. Registering checks unconditionally would instead make every
        // WebApplicationFactory test open a TCP connection to a PostgreSQL that the InMemory
        // provider replaced.
        services.AddHealthChecks();

        // Configured through the DI options rather than passed to MapHealthChecks below, so
        // every consumer of HealthCheckOptions sees the same writer.
        services.Configure<HealthCheckOptions>(options => options.ResponseWriter = HealthReportWriter.WriteAsync);

        services.AddOptions<HttpsRedirectionOptions>()
            .Bind(configuration.GetSection(HttpsRedirectionOptions.SectionName));

        if (configuration.IsServiceDiscoveryEnabled())
        {
            // The Postgres check and the Consul registration are gated by the SAME predicate.
            // Splitting them would let a host advertise itself to the registry with a health
            // check it never registered, or probe a database in a test that never reaches one.
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                services.AddTransient<NpgSqlHealthCheck>(_ => new NpgSqlHealthCheck(connectionString));
                services.AddHealthChecks()
                    .AddCheck<NpgSqlHealthCheck>("postgres");
            }

            // Registered BEFORE AddConsulServiceRegistration so its IServerPortResolver wins:
            // the extension uses TryAdd, so whoever registers first provides the answer.
            services.AddSingleton<IServerPortResolver, ServerAddressesPortResolver>();
            services.AddConsulServiceRegistration(configuration);
        }

        return services;
    }

    public static WebApplication UseApiMiddleware(this WebApplication app)
    {
        app.UseExceptionHandler();

        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.MapGet("/", () => Results.Redirect("/scalar/v1"));
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.Title = "Nurtricenter API";
            });
        }

        var httpsRedirectionOptions = app.Services
            .GetRequiredService<IOptions<HttpsRedirectionOptions>>().Value;

        if (httpsRedirectionOptions.Enabled)
        {
            // Only correct when THIS process is the TLS edge. Behind the platform gateway, TLS
            // is already terminated upstream and the proxy re-originates plain HTTP to this
            // port; redirecting here answers every forwarded request with a 307 to a port
            // nothing is listening on.
            app.UseHttpsRedirection();
        }

        app.UseAuthorization();

        app.MapControllers();

        // This is the URL ServiceRegistration hands to Consul as the instance's check, so the
        // endpoint and the registration are the same contract by construction.
        // AllowAnonymous: a probe carries no credentials.
        app.MapHealthChecks("/health").AllowAnonymous();

        return app;
    }
}
