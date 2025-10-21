using FluentAssertions;
using Raziee.SharedKernel.Domain.Events;
using Xunit;

namespace Raziee.SharedKernel.Tests.Domain.Events;

public class DomainEventTests
{
    [Fact]
    public void DomainEvent_ShouldHaveUniqueId()
    {
        // Arrange & Act
        var event1 = new TestDomainEvent();
        var event2 = new TestDomainEvent();

        // Assert
        event1.Id.Should().NotBe(event2.Id);
    }

    [Fact]
    public void DomainEvent_ShouldHaveOccurredOnTimestamp()
    {
        // Arrange
        var beforeEvent = DateTimeOffset.UtcNow;

        // Act
        var domainEvent = new TestDomainEvent();
        var afterEvent = DateTimeOffset.UtcNow;

        // Assert
        domainEvent.OccurredOn.Should().BeOnOrAfter(beforeEvent);
        domainEvent.OccurredOn.Should().BeOnOrBefore(afterEvent);
    }

    [Fact]
    public void DomainEvent_WithVersion_ShouldSetVersion()
    {
        // Arrange
        var version = 5;

        // Act
        var domainEvent = new TestDomainEvent(version);

        // Assert
        domainEvent.Version.Should().Be(version);
    }

    private class TestDomainEvent : DomainEvent
    {
        public TestDomainEvent() : base()
        {
        }

        public TestDomainEvent(int version) : base(version)
        {
        }
    }
}
