using BuildingBlocks.Domain;

namespace BuildingBlocks.Domain.Tests;

public class AggregateRootTests
{
    [Fact]
    public void Raised_events_are_collected_until_cleared()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.DoSomething();

        aggregate.DomainEvents.Count.ShouldBe(1);

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.ShouldBeEmpty();
    }

    private sealed record SomethingHappened(DateTimeOffset OccurredAt) : IDomainEvent;

    private sealed class TestAggregate(Guid id) : AggregateRoot<Guid>(id)
    {
        public void DoSomething() => Raise(new SomethingHappened(DateTimeOffset.UtcNow));
    }
}
