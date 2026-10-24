namespace Nurtricenter.Test.Infrastructure;

using FluentAssertions;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using Joseco.Outbox.EFCore.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nurtricenter.Core.Domain.Courier;
using Nurtricenter.Core.Domain.Courier.Events;
using Nurtricenter.Infrastructure.Data;

/// <summary>
/// Exercises the real <see cref="UnitOfWork"/> against the real <see cref="AppDbContext"/> on the
/// InMemory provider, with the real MediatR pipeline and the real SaveInOutbox handlers — which
/// also proves <c>AddOutboxModel</c> works on InMemory.
/// </summary>
public class UnitOfWorkTests
{
    private static ServiceProvider BuildProvider(string dbName, bool registerReentrantHandler = false)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(Nurtricenter.Application.DependencyInjection).Assembly));

        // Mirror AddApplication(): the publish handlers belong to the worker host only.
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
        services.AddScoped<IOutboxRepository<DomainEvent>>(sp => sp.GetRequiredService<EfCoreOutboxService<DomainEvent>>());

        if (registerReentrantHandler)
        {
            services.AddTransient<INotificationHandler<CourierCreatedEvent>, ReentrantCommitHandler>();
        }

        return services.BuildServiceProvider();
    }

    private static Courier NewCourier() => new(Guid.NewGuid(), "Jane Doe");

    [Fact]
    public async Task CommitAsync_WithEntityRaisingDomainEvent_SavesAggregateAndOutboxRowInOneCommit()
    {
        await using var provider = BuildProvider("Db_" + Guid.NewGuid());
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        context.Couriers.Add(NewCourier());
        await unitOfWork.CommitAsync();

        context.Couriers.Should().ContainSingle();
        context.OutboxMessages.Should().ContainSingle();
        var row = context.OutboxMessages.Single();
        row.Processed.Should().BeFalse();
        row.Content.Should().BeOfType<CourierCreatedEvent>();
    }

    [Fact]
    public async Task CommitAsync_CalledTwice_DoesNotReenter()
    {
        await using var provider = BuildProvider("Db_" + Guid.NewGuid(), registerReentrantHandler: true);
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        context.Couriers.Add(NewCourier());
        await unitOfWork.CommitAsync();
        await unitOfWork.CommitAsync();

        context.Couriers.Should().ContainSingle();
        context.OutboxMessages.Should().ContainSingle();
    }

    [Fact]
    public async Task CommitAsync_WhenSaveFails_ClearsTheChangeTracker()
    {
        var dbName = "Db_" + Guid.NewGuid();
        var duplicateId = Guid.NewGuid();

        // Seed the store with a courier holding the id we will duplicate, through a SEPARATE
        // context: within a single context the identity conflict would throw at Add(), never
        // reaching the unit of work's SaveChanges — and never exercising the tracker cleanup.
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(dbName).Options;
        await using (var seedContext = new AppDbContext(options))
        {
            seedContext.Couriers.Add(new Courier(duplicateId, "Seeded"));
            await seedContext.SaveChangesAsync();
        }

        await using var provider = BuildProvider(dbName);
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        context.Couriers.Add(new Courier(duplicateId, "Duplicate"));

        var act = () => unitOfWork.CommitAsync();

        await act.Should().ThrowAsync<Exception>();
        context.ChangeTracker.Entries().Should().BeEmpty();
        context.Couriers.Should().ContainSingle().Which.FullName.Should().Be("Seeded");
        context.OutboxMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task CommitAsync_ClearsDomainEventsFromTheEntities()
    {
        await using var provider = BuildProvider("Db_" + Guid.NewGuid());
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var courier = NewCourier();
        context.Couriers.Add(courier);
        await unitOfWork.CommitAsync();

        courier.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task CommitAsync_SecondCommit_DoesNotStageDuplicateOutboxRows()
    {
        await using var provider = BuildProvider("Db_" + Guid.NewGuid());
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        context.Couriers.Add(NewCourier());
        await unitOfWork.CommitAsync();

        await unitOfWork.CommitAsync();

        context.OutboxMessages.Should().ContainSingle();
    }

    [Fact]
    public void GetOutboxMessages_ReturnsTheContextsOutboxSet()
    {
        using var scope = BuildProvider("Db_" + Guid.NewGuid()).CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>() as IOutboxDatabase<DomainEvent>;

        unitOfWork!.GetOutboxMessages().Should().BeSameAs(context.OutboxMessages);
    }

    private sealed class ReentrantCommitHandler(IUnitOfWork unitOfWork) : INotificationHandler<CourierCreatedEvent>
    {
        public async Task Handle(CourierCreatedEvent notification, CancellationToken cancellationToken)
        {
            // Simulates CompleteRouteWhenAllDeliveriesCompletedHandler's nested commit: must be a
            // no-op while the outer commit is draining, not a deadlock or a second save.
            await unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
