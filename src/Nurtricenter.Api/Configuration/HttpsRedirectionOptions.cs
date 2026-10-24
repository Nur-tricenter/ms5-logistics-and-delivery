namespace Nurtricenter.Api.Configuration;

/// <summary>
/// Controls whether the API host redirects HTTP requests to HTTPS.
/// </summary>
/// <remarks>
/// Disabled by default: behind the platform gateway (or a compose stack), TLS is terminated
/// upstream and redirecting here would answer every forwarded request with a 307 to a port
/// nothing is listening on. Local development over the https launch profile enables it
/// explicitly in <c>appsettings.Development.json</c>.
/// </remarks>
public class HttpsRedirectionOptions
{
    public const string SectionName = "HttpsRedirection";

    public bool Enabled { get; set; }
}
