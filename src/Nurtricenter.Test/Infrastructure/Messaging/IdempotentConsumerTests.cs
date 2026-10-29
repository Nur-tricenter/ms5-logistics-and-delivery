namespace Nurtricenter.Test.Infrastructure.Messaging;

using FluentAssertions;
using Joseco.Communication.External.Contracts.Services;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using Joseco.Outbox.EFCore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nur.Tricenter.Integration.Messages.Contracting;
using Nurtricenter.Core.Domain.Courier;
using Nurtricenter.Infrastructure.Data;
using Nurtricenter.Infrastructure.Messaging;

/// <summary>
/// Runs the inbox decorator against the real <see cref="AppDbContext"/> (InMemory) and the real
/// <see cref="UnitOfWork"/>, with an inner consumer that commits through the unit of work exactly
/// like <see cref="PackageValidatedConsumer"/> does (via its command handler).
/// </summary>
public class IdempotentConsumerTests
{
    private static ServiceProvider BuildProvider(string dbName, bool innerThrows = false)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(Nurtricenter.Application.DependencyInjection).Assembly));

        // Mirror AddApplication(): the publish handlers belong to the outbox processor, not to this path.
        foreach (var descriptor in services
                     .Where(d => Nurtricenter.Application.DependencyInjection.IsOutboxPublishHandler(d.ServiceType))
                     .ToList())
        {
            services.Remove(descriptor);
        }

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOutboxDatabase<DomainEvent>>(sp => (UnitOfWork)sp.GetRequiredService<IUnitOfWork>());
        services.AddScoped<EfCoreOutboxService<DomainEvent>>();
        services.AddScoped<IOutboxService<DomainEvent>>(sp => sp.GetRequiredService<EfCoreOutboxService<DomainEvent>>());

        services.AddScoped(sp => new CourierCreatingConsumer(sp.GetRequiredService<AppDbContext>(), sp.GetRequiredService<IUnitOfWork>(), innerThrows));
        services.AddScoped<IdempotentConsumer<PackageValidated, CourierCreatingConsumer>>();

        return services.BuildServiceProvider();
    }

    private static PackageValidated MakeMessage() => new("corr-1", "contracting") { PackageId = Guid.NewGuid() };

    private static async Task DeliverAsync(ServiceProvider provider, PackageValidated message)
    {
        // One scope per delivery, as the RabbitMQ consumer does.
        await using var scope = provider.CreateAsyncScope();
        var consumer = scope.ServiceProvider.GetRequiredService<IdempotentConsumer<PackageValidated, CourierCreatingConsumer>>();
        await consumer.HandleAsync(message);
    }

    private static async Task<(int Couriers, int Inbox)> CountAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await context.Couriers.CountAsync(), await context.InboxMessages.CountAsync());
    }

    [Fact]
    public async Task HandleAsync_FirstDelivery_RunsTheInnerConsumerAndClaimsTheMessageInTheSameCommit()
    {
        await using var provider = BuildProvider("Db_" + Guid.NewGuid());
        var message = MakeMessage();

        await DeliverAsync(provider, message);

        (await CountAsync(provider)).Should().Be((1, 1));

        await using var scope = provider.CreateAsyncScope();
        var inbox = await scope.ServiceProvider.GetRequiredService<AppDbContext>().InboxMessages.SingleAsync();
        inbox.MessageId.Should().Be(message.Id);
        inbox.MessageType.Should().Be(nameof(PackageValidated));
        inbox.CorrelationId.Should().Be("corr-1");
    }

    [Fact]
    public async Task HandleAsync_Redelivery_IsSkipped()
    {
        await using var provider = BuildProvider("Db_" + Guid.NewGuid());
        var message = MakeMessage();

        await DeliverAsync(provider, message);
        await DeliverAsync(provider, message);

        (await CountAsync(provider)).Should().Be((1, 1));
    }

    [Fact]
    public async Task HandleAsync_WhenTheInnerConsumerFails_PersistsNothing_SoTheRedeliveryIsProcessed()
    {
        var dbName = "Db_" + Guid.NewGuid();
        var message = MakeMessage();

        await using (var failing = BuildProvider(dbName, innerThrows: true))
        {
            var act = () => DeliverAsync(failing, message);
            await act.Should().ThrowAsync<InvalidOperationException>();
            (await CountAsync(failing)).Should().Be((0, 0));
        }

        await using var healthy = BuildProvider(dbName);
        await DeliverAsync(healthy, message);

        (await CountAsync(healthy)).Should().Be((1, 1));
    }

    /// <summary>Creates a courier and commits through the unit of work, or fails during the commit.</summary>
    private sealed class CourierCreatingConsumer(AppDbContext context, IUnitOfWork unitOfWork, bool throws)
        : IIntegrationMessageConsumer<PackageValidated>
    {
        public async Task HandleAsync(PackageValidated message, CancellationToken cancellationToken = default)
        {
            context.Couriers.Add(new Courier(Guid.NewGuid(), "Jane Doe"));

            if (throws)
            {
                // Mirrors a handler whose commit fails: the unit of work clears the tracker and rethrows.
                context.ChangeTracker.Clear();
                throw new InvalidOperationException("Simulated failure.");
            }

            await unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
