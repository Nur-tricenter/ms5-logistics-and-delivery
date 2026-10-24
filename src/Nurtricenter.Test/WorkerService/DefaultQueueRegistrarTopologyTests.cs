namespace Nurtricenter.Test.WorkerService;

using FluentAssertions;
using Nurtricenter.Contracts.Integration;
using Nurtricenter.WorkerService;

/// <summary>
/// The registrar must bind the history sink queue to EXACTLY the registry's exchange list: a
/// missing binding silently loses history, a stale binding references an exchange nobody
/// publishes to.
/// </summary>
public class DefaultQueueRegistrarTopologyTests
{
    [Fact]
    public void QueueName_IsTheRegistryDefaultQueue()
    {
        DefaultQueueRegistrar.QueueName.Should().Be(NurtricenterExchanges.DefaultQueue);
        DefaultQueueRegistrar.QueueName.Should().Be("nurtricenter.default");
    }

    [Fact]
    public void BoundExchanges_AreExactlyTheRegistryExchanges()
    {
        DefaultQueueRegistrar.BoundExchanges.Should().Equal(NurtricenterExchanges.All);
        DefaultQueueRegistrar.BoundExchanges.Should().HaveCount(10);
    }
}
