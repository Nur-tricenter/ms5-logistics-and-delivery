namespace Nurtricenter.Api.Utils;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Serialises a <see cref="HealthReport"/> as JSON for <c>/health</c>.
/// </summary>
/// <remarks>
/// The default writer emits bare text (<c>Healthy</c> / <c>Unhealthy</c>), which is unreadable
/// for anything that has to decide WHY an instance is unhealthy. Consul only cares about the
/// status code, so the plain-text default would technically satisfy the check — but the same
/// endpoint is what a human runs during an incident.
/// </remarks>
public static class HealthReportWriter
{
    public static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            success = report.Status == HealthStatus.Healthy,
            message = report.Status.ToString(),
            data = new
            {
                status = report.Status.ToString(),
                totalDurationMilliseconds = report.TotalDuration.TotalMilliseconds,
                entries = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    durationMilliseconds = entry.Value.Duration.TotalMilliseconds,
                    error = entry.Value.Exception?.Message
                })
            }
        };

        await context.Response.WriteAsJsonAsync(payload);
    }
}
