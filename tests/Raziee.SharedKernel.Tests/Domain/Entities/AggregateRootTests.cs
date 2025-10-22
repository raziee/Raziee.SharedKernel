using FluentAssertions;
using Raziee.SharedKernel.Domain.Entities;
using Raziee.SharedKernel.Domain.Events;
using Xunit;

namespace Raziee.SharedKernel.Tests.Domain.Entities;

public class AggregateRootTests
{
    [Fact]
    public void AggregateRoot_ShouldStartWithNoDomainEvents()
    {
        // Arrange & Act
        var aggregateRoot = new TestAggregateRoot(Guid.NewGuid());

        // Assert
        aggregateRoot.DomainEvents.Should().BeEmpty();
        aggregateRoot.HasDomainEvents().Should().BeFalse();
    }

    [Fact]
    public void AggregateRoot_AddDomainEvent_ShouldAddEvent()
    {
        // Arrange
        var aggregateRoot = new TestAggregateRoot(Guid.NewGuid());
        var domainEvent = new TestDomainEvent();

        // Act
        aggregateRoot.AddDomainEvent(domainEvent);

        // Assert
        aggregateRoot.DomainEvents.Should().Contain(domainEvent);
        aggregateRoot.HasDomainEvents().Should().BeTrue();
    }

    [Fact]
    public void AggregateRoot_RemoveDomainEvent_ShouldRemoveEvent()
    {
        // Arrange
        var aggregateRoot = new TestAggregateRoot(Guid.NewGuid());
        var domainEvent = new TestDomainEvent();
        aggregateRoot.AddDomainEvent(domainEvent);

        // Act
        aggregateRoot.RemoveDomainEvent(domainEvent);

        // Assert
        aggregateRoot.DomainEvents.Should().NotContain(domainEvent);
        aggregateRoot.HasDomainEvents().Should().BeFalse();
    }

    [Fact]
    public void AggregateRoot_ClearDomainEvents_ShouldClearAllEvents()
    {
        // Arrange
        var aggregateRoot = new TestAggregateRoot(Guid.NewGuid());
        aggregateRoot.AddDomainEvent(new TestDomainEvent());
        aggregateRoot.AddDomainEvent(new TestDomainEvent());

        // Act
        aggregateRoot.ClearDomainEvents();

        // Assert
        aggregateRoot.DomainEvents.Should().BeEmpty();
        aggregateRoot.HasDomainEvents().Should().BeFalse();
    }

    private class TestAggregateRoot : AggregateRoot<Guid>
    {
        public TestAggregateRoot(Guid id)
            : base(id) { }

        public new void AddDomainEvent(IDomainEvent domainEvent)
        {
            base.AddDomainEvent(domainEvent);
        }

        public new void RemoveDomainEvent(IDomainEvent domainEvent)
        {
            base.RemoveDomainEvent(domainEvent);
        }
    }

    private class TestDomainEvent : DomainEvent { }
}
