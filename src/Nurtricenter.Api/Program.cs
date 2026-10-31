using Microsoft.EntityFrameworkCore;
using Nurtricenter.Api;
using Nurtricenter.Application;
using Nurtricenter.Infrastructure;
using Nurtricenter.Infrastructure.Data;
using Serilog;

// `--migrate` is a bare switch, which the command-line configuration provider rejects ("unrecognized
// argument format"), so it is stripped before the host sees the arguments.
var migrateOnly = args.Any(a => string.Equals(a, "--migrate", StringComparison.OrdinalIgnoreCase));
args = args.Where(a => !string.Equals(a, "--migrate", StringComparison.OrdinalIgnoreCase)).ToArray();

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices(builder.Configuration);

var app = builder.Build();

// `--migrate`: one-shot job (Nur-Tricenter platform convention, see nurtricenter.compose.yml). Applies EF
// Core migrations and exits 0/1 without starting the web host, so Consul registration and the HTTP
// listener never start for the job container.
if (migrateOnly)
{
    try
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        Log.Information("Migrations applied");
        return 0;
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Migration failed");
        return 1;
    }
    finally
    {
        Log.CloseAndFlush();
    }
}

app.UseApiMiddleware();

try
{
    Log.Information("Starting Nurtricenter API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

return 0;

public partial class Program { }
