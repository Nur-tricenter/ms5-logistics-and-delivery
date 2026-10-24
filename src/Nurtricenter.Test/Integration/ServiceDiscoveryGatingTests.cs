namespace Nurtricenter.Test.Integration;

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nurtricenter.Infrastructure.ServiceDiscovery;

/// <summary>
/// Guards the factory override that keeps the test host off the developer's live Consul registry
/// and off the real database health probe: the single predicate must read false inside the test
/// host even though appsettings.Development.json points at a real Consul agent.
/// </summary>
[Collection("Integration")]
public class ServiceDiscoveryGatingTests : BaseIntegrationTest
{
    public ServiceDiscoveryGatingTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public void ServiceDiscovery_IsDisabled_InTheTestHost()
    {
        var configuration = Factory.Services.GetRequiredService<IConfiguration>();

        configuration.IsServiceDiscoveryEnabled().Should().BeFalse();
    }
}
