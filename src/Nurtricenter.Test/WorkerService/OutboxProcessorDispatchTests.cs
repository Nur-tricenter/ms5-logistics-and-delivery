namespace Nurtricenter.Test.WorkerService;

using FluentAssertions;
using Joseco.Communication.External.Contracts.Services;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.EFCore.Procesor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Nurtricenter.Application;
using Nur.Tricenter.Integration.Messages.Logistics;
using Nurtricenter.Contracts.Integration;
using Nurtricenter.Core.Domain.Courier;
using Nurtricenter.Infrastructure;
using Nurtricenter.Infrastructure.Data;

/// <summary>
/// Runs the REAL Joseco <see cref="OutboxProcessor{E}"/> against the worker's real DI wiring
/// (only the database and the broker are substituted). This is the regression guard for the
/// dispatch contract: the processor publishes each row as
/// <c>OutboxMessage&lt;{runtime event type}&gt;</c>, and a handler registered for any other
/// type is silently skipped while the row is still marked as processed.
/// </summary>
public class OutboxProcessorDispatchTests
{
    [Fact]
    public async Task Process_PublishesTheStagedEventAndMarksTheRowProcessed()
    {
        var dbName = "OutboxDispatch_" + Guid.NewGuid();
        var publisher = new Mock<IExternalPublisher>();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ClinicService:BaseUrl"] = "http://localhost"
            })
            .Build());
        services.AddIntegrationMessaging();

        // Swap the database for InMemory and the broker for a mock.
        services.RemoveAll<DbContextOptions<AppDbContext>>();
        services.AddSingleton(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(dbName).Options);
        services.AddSingleton(publisher.Object);

        await using var provider = services.BuildServiceProvider();

        // API side: create a courier through the real unit of work, staging the outbox row.
        var courierId = Guid.NewGuid();
        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Couriers.Add(new Courier(courierId, "Jane Doe"));
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync();
        }

        // Worker side: one polling cycle of the library's processor.
        using (var scope = provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<OutboxProcessor<DomainEvent>>().Process(CancellationToken.None);
        }

        publisher.Verify(p => p.PublishAsync(
            It.Is<CourierCreated>(c => c.CourierId == courierId && c.FullName == "Jane Doe"),
            NurtricenterExchanges.CourierCreated,
            true), Times.Once);

        using (var scope = provider.CreateScope())
        {
            var row = scope.ServiceProvider.GetRequiredService<AppDbContext>().OutboxMessages.Single();
            row.Processed.Should().BeTrue();
        }
    }
}
