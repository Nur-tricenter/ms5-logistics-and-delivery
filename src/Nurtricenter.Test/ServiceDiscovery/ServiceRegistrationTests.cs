namespace Nurtricenter.Test.ServiceDiscovery;

using Consul;
using FluentAssertions;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Nurtricenter.Infrastructure.ServiceDiscovery;

public class ServiceRegistrationTests
{
    private readonly Mock<IConsulClient> _consulClientMock = new();
    private readonly Mock<IAgentEndpoint> _agentMock = new();
    private readonly Mock<IServerPortResolver> _portResolverMock = new();

    public ServiceRegistrationTests()
    {
        _consulClientMock.Setup(c => c.Agent).Returns(_agentMock.Object);
        _agentMock
            .Setup(a => a.ServiceRegister(It.IsAny<AgentServiceRegistration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WriteResult<AgentServiceRegistration>());
        _agentMock
            .Setup(a => a.ServiceDeregister(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WriteResult<bool>());
    }

    private ServiceRegistration BuildSut(ServiceRegistrationOptions options) =>
        new(
            _consulClientMock.Object,
            Options.Create(options),
            _portResolverMock.Object,
            NullLogger<ServiceRegistration>.Instance);

    [Fact]
    public async Task StartAsync_WithValidConfiguration_RegistersServiceWithHealthCheckOnOwnHealthEndpoint()
    {
        AgentServiceRegistration? captured = null;
        _agentMock
            .Setup(a => a.ServiceRegister(It.IsAny<AgentServiceRegistration>(), It.IsAny<CancellationToken>()))
            .Callback<AgentServiceRegistration, CancellationToken>((registration, _) => captured = registration)
            .ReturnsAsync(new WriteResult<AgentServiceRegistration>());

        var sut = BuildSut(new ServiceRegistrationOptions
        {
            ServiceDiscoveryAddress = "http://localhost:8500",
            ServiceId = "nurtricenter-service-1",
            ServiceName = "nurtricenter-service",
            Host = "nurtricenter",
            Port = 8080
        });

        await sut.StartAsync(CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.ID.Should().Be("nurtricenter-service-1");
        captured.Name.Should().Be("nurtricenter-service");
        captured.Address.Should().Be("nurtricenter");
        captured.Port.Should().Be(8080);
        captured.Check!.HTTP.Should().Be("http://nurtricenter:8080/health");
    }

    [Fact]
    public async Task StartAsync_WithValidConfiguration_AdvertisesDeregisterCriticalServiceAfter30Seconds()
    {
        AgentServiceRegistration? captured = null;
        _agentMock
            .Setup(a => a.ServiceRegister(It.IsAny<AgentServiceRegistration>(), It.IsAny<CancellationToken>()))
            .Callback<AgentServiceRegistration, CancellationToken>((registration, _) => captured = registration)
            .ReturnsAsync(new WriteResult<AgentServiceRegistration>());

        var sut = BuildSut(new ServiceRegistrationOptions
        {
            ServiceDiscoveryAddress = "http://localhost:8500",
            ServiceId = "nurtricenter-service-1",
            ServiceName = "nurtricenter-service",
            Host = "nurtricenter",
            Port = 8080
        });

        await sut.StartAsync(CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Check!.DeregisterCriticalServiceAfter.Should().Be(TimeSpan.FromSeconds(30));
        captured.Check.DeregisterCriticalServiceAfter.Should().Be(ServiceRegistration.DeregisterCriticalServiceAfter);
    }

    [Fact]
    public async Task StopAsync_AfterSuccessfulRegistration_DeregistersTheService()
    {
        var sut = BuildSut(new ServiceRegistrationOptions
        {
            ServiceDiscoveryAddress = "http://localhost:8500",
            ServiceId = "nurtricenter-service-1",
            ServiceName = "nurtricenter-service",
            Host = "nurtricenter",
            Port = 8080
        });
        await sut.StartAsync(CancellationToken.None);

        await sut.StopAsync(CancellationToken.None);

        _agentMock.Verify(a => a.ServiceDeregister("nurtricenter-service-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WithoutServiceDiscoveryAddress_DoesNotRegisterAndDoesNotThrow()
    {
        var sut = BuildSut(new ServiceRegistrationOptions
        {
            ServiceId = "nurtricenter-service-1",
            ServiceName = "nurtricenter-service",
            Host = "localhost",
            Port = 8080
        });

        var act = () => sut.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        _agentMock.Verify(
            a => a.ServiceRegister(It.IsAny<AgentServiceRegistration>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task StartAsync_WhenConsulThrows_RetriesThreeTimesAndNeverThrows()
    {
        _agentMock
            .Setup(a => a.ServiceRegister(It.IsAny<AgentServiceRegistration>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConsulRequestException("consul is down", HttpStatusCode.InternalServerError));

        var sut = BuildSut(new ServiceRegistrationOptions
        {
            ServiceDiscoveryAddress = "http://localhost:8500",
            ServiceId = "nurtricenter-service-1",
            ServiceName = "nurtricenter-service",
            Host = "nurtricenter",
            Port = 8080
        });

        var act = () => sut.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        _agentMock.Verify(
            a => a.ServiceRegister(It.IsAny<AgentServiceRegistration>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task StartAsync_WithoutConfiguredPort_UsesTheBoundPort()
    {
        AgentServiceRegistration? captured = null;
        _agentMock
            .Setup(a => a.ServiceRegister(It.IsAny<AgentServiceRegistration>(), It.IsAny<CancellationToken>()))
            .Callback<AgentServiceRegistration, CancellationToken>((registration, _) => captured = registration)
            .ReturnsAsync(new WriteResult<AgentServiceRegistration>());
        _portResolverMock.Setup(r => r.ResolveBoundPort()).Returns(5036);

        var sut = BuildSut(new ServiceRegistrationOptions
        {
            ServiceDiscoveryAddress = "http://localhost:8500",
            ServiceId = "nurtricenter-service-1",
            ServiceName = "nurtricenter-service",
            Host = "localhost",
            Port = 0
        });

        await sut.StartAsync(CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Port.Should().Be(5036);
    }

    [Fact]
    public async Task StartAsync_UnderMinimalHosting_DefersRegistrationUntilTheServerHasBound()
    {
        // Minimal hosting starts hosted services BEFORE Kestrel listens: the bound port is unknown
        // during StartAsync and only becomes readable once ApplicationStarted fires.
        int? boundPort = null;
        _portResolverMock.Setup(r => r.ResolveBoundPort()).Returns(() => boundPort);

        using var started = new CancellationTokenSource();
        var lifetime = new Mock<IHostApplicationLifetime>();
        lifetime.Setup(l => l.ApplicationStarted).Returns(started.Token);
        lifetime.Setup(l => l.ApplicationStopping).Returns(CancellationToken.None);

        AgentServiceRegistration? captured = null;
        _agentMock
            .Setup(a => a.ServiceRegister(It.IsAny<AgentServiceRegistration>(), It.IsAny<CancellationToken>()))
            .Callback<AgentServiceRegistration, CancellationToken>((registration, _) => captured = registration)
            .ReturnsAsync(new WriteResult<AgentServiceRegistration>());

        var sut = new ServiceRegistration(
            _consulClientMock.Object,
            Options.Create(new ServiceRegistrationOptions
            {
                ServiceDiscoveryAddress = "http://localhost:8500",
                ServiceId = "nurtricenter-service-1",
                ServiceName = "nurtricenter-service",
                Host = "host.docker.internal",
                Port = 0
            }),
            _portResolverMock.Object,
            NullLogger<ServiceRegistration>.Instance,
            lifetime.Object);

        await sut.StartAsync(CancellationToken.None);
        captured.Should().BeNull("the server has not bound a port yet");

        boundPort = 5036;
        started.Cancel();

        captured.Should().NotBeNull();
        captured!.Port.Should().Be(5036);
        captured.Check!.HTTP.Should().Be("http://host.docker.internal:5036/health");
    }

    [Fact]
    public async Task StartAsync_WithoutAnyResolvablePort_DoesNotRegisterAndDoesNotThrow()
    {
        _portResolverMock.Setup(r => r.ResolveBoundPort()).Returns((int?)null);

        var sut = BuildSut(new ServiceRegistrationOptions
        {
            ServiceDiscoveryAddress = "http://localhost:8500",
            ServiceId = "nurtricenter-service-1",
            ServiceName = "nurtricenter-service",
            Host = "localhost",
            Port = 0
        });

        var act = () => sut.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        _agentMock.Verify(
            a => a.ServiceRegister(It.IsAny<AgentServiceRegistration>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void AddConsulServiceRegistration_WithoutDiscoveryAddress_RegistersNothing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddConsulServiceRegistration(configuration);

        services.Should().NotContain(d => d.ServiceType == typeof(IConsulClient));
        services.Should().NotContain(d => d.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void AddConsulServiceRegistration_WithDiscoveryAddress_RegistersClientAndHostedService()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceRegistration:ServiceDiscoveryAddress"] = "http://localhost:8500"
            })
            .Build();

        services.AddConsulServiceRegistration(configuration);

        services.Should().Contain(d => d.ServiceType == typeof(IConsulClient));
        services.Should().Contain(d => d.ServiceType == typeof(IHostedService));
    }
}
