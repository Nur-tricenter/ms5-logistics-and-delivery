namespace Nurtricenter.Test.Infrastructure.Outbox;

using System.Diagnostics;
using FluentAssertions;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using Moq;
using Nurtricenter.Infrastructure.Observability;

public class OutboxTracingServiceTests
{
    private readonly Mock<IOutboxService<DomainEvent>> _innerMock = new();

    private OutboxTracingService<DomainEvent> BuildSut() => new(_innerMock.Object);

    [Fact]
    public async Task AddAsync_WithAmbientActivity_StampsTraceAndCorrelationIds()
    {
        using var activity = new Activity("test-operation");
        activity.Start();

        OutboxMessage<DomainEvent>? staged = null;
        _innerMock
            .Setup(s => s.AddAsync(It.IsAny<OutboxMessage<DomainEvent>>()))
            .Callback<OutboxMessage<DomainEvent>>(message => staged = message);

        var sut = BuildSut();
        var message = new OutboxMessage<DomainEvent>(new TestDomainEvent());

        await sut.AddAsync(message);

        staged.Should().BeSameAs(message);
        message.TraceId.Should().Be(activity.TraceId.ToString());
        message.SpanId.Should().Be(activity.SpanId.ToString());
        message.CorrelationId.Should().Be(activity.TraceId.ToString());
    }

    [Fact]
    public async Task AddAsync_WithoutAmbientActivity_LeavesTraceIdsNull()
    {
        Activity.Current = null;

        var sut = BuildSut();
        var message = new OutboxMessage<DomainEvent>(new TestDomainEvent());

        await sut.AddAsync(message);

        message.TraceId.Should().BeNull();
        message.SpanId.Should().BeNull();
        message.CorrelationId.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_WithExistingCorrelationId_PreservesIt()
    {
        using var activity = new Activity("test-operation");
        activity.Start();

        var sut = BuildSut();
        var message = new OutboxMessage<DomainEvent>(new TestDomainEvent())
        {
            CorrelationId = "caller-supplied"
        };

        await sut.AddAsync(message);

        message.CorrelationId.Should().Be("caller-supplied");
    }

    private sealed record TestDomainEvent : DomainEvent;
}
