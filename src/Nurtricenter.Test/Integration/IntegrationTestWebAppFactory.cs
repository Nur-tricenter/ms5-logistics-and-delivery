namespace Nurtricenter.Test.Integration;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Nurtricenter.Core.Interfaces.Services.ClinicService;
using Nurtricenter.Infrastructure.Data;
using Nurtricenter.Infrastructure.ServiceDiscovery;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>
{
    public Mock<IClinicService> ClinicServiceMock { get; } = new Mock<IClinicService> { DefaultValue = DefaultValue.Mock };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Blanks ServiceRegistration:ServiceDiscoveryAddress for the test host. This is not
        // optional politeness, it is what keeps the suite hermetic: WebApplicationFactory runs the
        // real host in the Development environment with the content root set to
        // src/Nurtricenter.Api, so appsettings.Development.json — which points at the real Consul
        // on http://localhost:8500 — is loaded by every integration test. Without this override
        // each test would register a phantom service in the developer's live registry and probe
        // the real database through the postgres health check.
        //
        // The override is appended as an in-memory configuration source rather than issued through
        // UseSetting: UseSetting does NOT win — its values are merged into the host configuration
        // with LOWER precedence than the appsettings files. A provider appended last through
        // ConfigureAppConfiguration does win.
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var key = $"{ServiceRegistrationOptions.SectionName}:{nameof(ServiceRegistrationOptions.ServiceDiscoveryAddress)}";
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [key] = string.Empty
            });
        });

        builder.ConfigureTestServices(services =>
        {
            var efServices = services.Where(s => s.ServiceType.Namespace != null && s.ServiceType.Namespace.StartsWith("Microsoft.EntityFrameworkCore")).ToList();
            foreach (var s in efServices) services.Remove(s);
            
            var npgsqlServices = services.Where(s => s.ServiceType.Namespace != null && s.ServiceType.Namespace.StartsWith("Npgsql")).ToList();
            foreach (var s in npgsqlServices) services.Remove(s);

            string dbName = "TestDb_" + Guid.NewGuid().ToString();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            services.RemoveAll(typeof(IClinicService));
            services.AddSingleton(ClinicServiceMock.Object);
        });
    }
}
