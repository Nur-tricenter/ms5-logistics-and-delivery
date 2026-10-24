namespace Nurtricenter.Test.Infrastructure.Outbox;

using FluentAssertions;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.EFCore.Persistence;
using Microsoft.EntityFrameworkCore;
using Nurtricenter.Infrastructure.Data;

public class EfCoreOutboxServiceTests
{
    private sealed record TestDomainEvent : DomainEvent;

    private sealed class StubOutboxDatabase(AppDbContext context) : IOutboxDatabase<DomainEvent>
    {
        public DbSet<OutboxMessage<DomainEvent>> GetOutboxMessages() => context.OutboxMessages;

        // Only the real UnitOfWork commits; the stub has nothing to persist beyond the context.
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static AppDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("OutboxTestDb_" + Guid.NewGuid())
            .Options;
        return new AppDbContext(options);
    }

    private static OutboxMessage<DomainEvent> NewMessage() => new(new TestDomainEvent());

    [Fact]
    public async Task AddAsync_StagesTheRowInTheChangeTracker_NothingIsWrittenUntilSave()
    {
        await using var context = BuildContext();
        var sut = new EfCoreOutboxService<DomainEvent>(new StubOutboxDatabase(context));

        await sut.AddAsync(NewMessage());

        context.ChangeTracker.HasChanges().Should().BeTrue();
        context.OutboxMessages.Local.Should().ContainSingle();

        await context.SaveChangesAsync();
        context.OutboxMessages.Count().Should().Be(1);
    }

    [Fact]
    public async Task Update_MarksTheRowAsModified()
    {
        await using var context = BuildContext();
        var message = NewMessage();
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();
        var sut = new EfCoreOutboxService<DomainEvent>(new StubOutboxDatabase(context));

        await sut.Update(message);

        context.Entry(message).State.Should().Be(EntityState.Modified);
    }

    [Fact]
    public async Task GetUnprocessedAsync_ReturnsOnlyUnprocessedRowsOrderedByCreation()
    {
        await using var context = BuildContext();
        var sut = new EfCoreOutboxService<DomainEvent>(new StubOutboxDatabase(context));

        var processed = NewMessage();
        processed.Processed = true;
        var older = NewMessage();
        var newer = NewMessage();

        context.OutboxMessages.AddRange(processed, older, newer);
        await context.SaveChangesAsync();

        var unprocessed = (await sut.GetUnprocessedAsync(10)).ToList();

        unprocessed.Should().HaveCount(2);
        unprocessed.Should().ContainInOrder(older, newer);
    }

    [Fact]
    public async Task GetUnprocessedAsync_RespectsPageSize()
    {
        await using var context = BuildContext();
        var sut = new EfCoreOutboxService<DomainEvent>(new StubOutboxDatabase(context));

        context.OutboxMessages.AddRange(NewMessage(), NewMessage(), NewMessage());
        await context.SaveChangesAsync();

        var page = (await sut.GetUnprocessedAsync(2)).ToList();

        page.Should().HaveCount(2);
    }
}
