namespace Nurtricenter.Test.Configuration;

using System.Text.Json;
using FluentAssertions;

/// <summary>
/// Credentials and discovery addresses must never reach the tracked configuration: they belong
/// to git-ignored appsettings.Development.json files and environment variables.
/// </summary>
public class TrackedConfigurationTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    private static JsonDocument Parse(string relativePath) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot, relativePath)));

    [Fact]
    public void Api_AppSettings_ContainsNoSecretsAndKeepsHttpsRedirectionOff()
    {
        using var document = Parse("src/Nurtricenter.Api/appsettings.json");
        var root = document.RootElement;

        root.TryGetProperty("ConnectionStrings", out _).Should().BeFalse();
        root.TryGetProperty("ServiceRegistration", out _).Should().BeFalse();
        root.GetProperty("HttpsRedirection").GetProperty("Enabled").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void Worker_AppSettings_ContainsNoSecrets()
    {
        using var document = Parse("src/Nurtricenter.WorkerService/appsettings.json");
        var root = document.RootElement;

        root.TryGetProperty("ConnectionStrings", out _).Should().BeFalse();
        root.TryGetProperty("RabbitMqSettings", out _).Should().BeFalse();
    }
}
