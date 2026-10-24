namespace Nurtricenter.Test.Contracts;

using FluentAssertions;
using Nurtricenter.Contracts.Integration;

/// <summary>
/// Registry guard: every publisher destination must be a declared exchange, every declared
/// exchange must have exactly one publisher, and the history sink queue is not an exchange.
/// </summary>
public class NurtricenterExchangesTests
{
    [Fact]
    public void All_ContainsExactlyTenExchanges_WithoutDuplicates()
    {
        NurtricenterExchanges.All.Should().HaveCount(10);
        NurtricenterExchanges.All.Distinct().Should().HaveCount(10);
    }

    [Fact]
    public void EveryPublisherDestination_IsARegisteredExchange_AndEveryExchangeHasAPublisher()
    {
        var applicationAssembly = typeof(Nurtricenter.Application.DependencyInjection).Assembly;

        var destinations = applicationAssembly.GetTypes()
            .Where(t => t.Name.StartsWith("PublishIntegrationEventWhen"))
            .Select(t => (string)t.GetField("Destination")!.GetValue(null)!)
            .ToList();

        destinations.Should().HaveCount(10);
        destinations.Should().OnlyContain(destination => NurtricenterExchanges.Contains(destination));
        destinations.Distinct().Should().HaveCount(10);
        destinations.Should().BeEquivalentTo(NurtricenterExchanges.All);
    }

    [Fact]
    public void DefaultQueue_IsNotAnExchange()
    {
        NurtricenterExchanges.All.Should().NotContain(NurtricenterExchanges.DefaultQueue);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("unknown-exchange", false)]
    [InlineData("courier-created", true)]
    [InlineData("delivery-failed", true)]
    public void Contains_AnswersForKnownValues(string? exchange, bool expected)
    {
        NurtricenterExchanges.Contains(exchange).Should().Be(expected);
    }
}
