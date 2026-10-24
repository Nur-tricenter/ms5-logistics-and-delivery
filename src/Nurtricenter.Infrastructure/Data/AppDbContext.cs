namespace Nurtricenter.Infrastructure.Data;

using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.EFCore.Persistence;
using Microsoft.EntityFrameworkCore;
using Nurtricenter.Core.Domain.Courier;
using Nurtricenter.Core.Domain.Route;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Courier> Couriers => Set<Courier>();
    public DbSet<Route> Routes => Set<Route>();

    /// <summary>
    /// Transactional outbox rows, persisted in the same transaction as the aggregates by
    /// <see cref="UnitOfWork"/>. The schema is fixed by Joseco.Outbox.EFCore (outbox.outboxMessage).
    /// </summary>
    public DbSet<OutboxMessage<DomainEvent>> OutboxMessages => Set<OutboxMessage<DomainEvent>>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // The base type is a MediatR notification, never a persisted entity.
        modelBuilder.Ignore<DomainEvent>();

        modelBuilder.AddOutboxModel<DomainEvent>();
    }
}
