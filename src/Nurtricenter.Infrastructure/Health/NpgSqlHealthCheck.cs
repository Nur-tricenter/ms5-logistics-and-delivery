namespace Nurtricenter.Infrastructure.Health;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

/// <summary>
/// Verifies the service can open a connection to PostgreSQL and run a trivial query.
/// </summary>
/// <remarks>
/// Written instead of the <c>AspNetCore.HealthChecks.NpgSql</c> package because that package's
/// newest stable version targets Npgsql 9 while this service is on Npgsql 10, which would force
/// a package downgrade. The check is registered under the name "postgres" and only when service
/// discovery is enabled, so <c>/health</c> reports 503 and Consul stops advertising the instance
/// while the database is unreachable.
/// </remarks>
public sealed class NpgSqlHealthCheck(string connectionString) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand("SELECT 1;", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is unreachable.", exception);
        }
    }
}
