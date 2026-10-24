namespace Nurtricenter.Test.DomainEvents;

using FluentAssertions;
using Joseco.DDD.Core.Abstractions;
using Nurtricenter.Core.Domain.Courier.Events;

/// <summary>
/// Reflection guard: every concrete domain event must have BOTH sides of the outbox chain — a
/// <c>SaveInOutboxWhen{Event}</c> handler (stages the row in the request transaction) and a
/// <c>PublishIntegrationEventWhen{Name}</c> handler (publishes it from the worker). An event
/// missing either side is silently dropped, so the guard fails the build instead.
/// </summary>
public class OutboxChainCoverageTests
{
    [Fact]
    public void EveryDomainEvent_HasSaveAndPublishHandlers()
    {
        var applicationAssembly = typeof(Nurtricenter.Application.DependencyInjection).Assembly;

        var domainEventTypes = typeof(CourierCreatedEvent).Assembly.GetTypes()
            .Where(t => !t.IsAbstract
                        && typeof(DomainEvent).IsAssignableFrom(t)
                        && (t.Namespace?.StartsWith("Nurtricenter.Core.Domain") ?? false))
            .ToList();

        domainEventTypes.Should().NotBeEmpty();

        foreach (var eventType in domainEventTypes)
        {
            applicationAssembly
                .GetType($"Nurtricenter.Application.DomainEvents.SaveInOutboxWhen{eventType.Name}")
                .Should().NotBeNull($"event {eventType.Name} needs a SaveInOutbox handler");

            var publishHandlerName = eventType.Name.EndsWith("Event", StringComparison.Ordinal)
                ? eventType.Name[..^"Event".Length]
                : eventType.Name;

            applicationAssembly
                .GetType($"Nurtricenter.Application.DomainEvents.PublishIntegrationEventWhen{publishHandlerName}")
                .Should().NotBeNull($"event {eventType.Name} needs a PublishIntegrationEvent handler");
        }
    }
}
