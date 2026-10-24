namespace Nurtricenter.Test.WorkerService;

using FluentAssertions;
using Joseco.Communication.External.Contracts.Services;
using Joseco.Communication.External.RabbitMQ;
using Joseco.Communication.External.RabbitMQ.Services;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nurtricenter.Application;
using Nurtricenter.WorkerService;

public class RabbitMqRegistrationTests
{
    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMqSettings:Host"] = "localhost",
                ["RabbitMqSettings:UserName"] = "guest",
                ["RabbitMqSettings:Password"] = "guest",
                ["RabbitMqSettings:VirtualHost"] = "/"
            })
            .Build();

    [Fact]
    public void AddNurtricenterRabbitMq_WithoutSection_Throws()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var act = () => services.AddNurtricenterRabbitMq(configuration);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddNurtricenterRabbitMq_WithSection_RegistersSettingsAndPublisher()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddNurtricenterRabbitMq(BuildConfiguration());

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<RabbitMqSettings>().Host.Should().Be("localhost");
        provider.GetRequiredService<IExternalPublisher>().Should().NotBeNull();
    }

    [Fact]
    public void AddApplication_WithoutAddIntegrationMessaging_RegistersNoOutboxPublishHandlers()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        services.Should().NotContain(d => Nurtricenter.Application.DependencyInjection.IsOutboxPublishHandler(d.ServiceType));
    }

    [Fact]
    public void AddIntegrationMessaging_RegistersTheTenPublishHandlers()
    {
        var services = new ServiceCollection();

        services.AddApplication();
        services.AddIntegrationMessaging();

        services
            .Where(d => Nurtricenter.Application.DependencyInjection.IsOutboxPublishHandler(d.ServiceType))
            .Should().HaveCount(10);
    }
}
