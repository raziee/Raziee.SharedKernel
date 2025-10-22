using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Raziee.SharedKernel.Domain.Events;
using Xunit;

namespace Raziee.SharedKernel.Tests.Domain.Events;

public class DomainEventDispatcherTests
{
    private readonly Mock<ILogger<DomainEventDispatcher>> _loggerMock;

    public DomainEventDispatcherTests()
    {
        _loggerMock = new Mock<ILogger<DomainEventDispatcher>>();
    }

    [Fact]
    public async Task DispatchAsync_WithSingleEvent_ShouldDispatchToHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        var eventHandler = new Mock<IDomainEventHandler<TestDomainEvent>>();
        services.AddSingleton(eventHandler.Object);

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(serviceProvider, _loggerMock.Object);

        var domainEvent = new TestDomainEvent("Test Value");
        var events = new[] { domainEvent };

        // Act
        await dispatcher.DispatchAsync(events);

        // Assert
        eventHandler.Verify(x => x.HandleAsync(domainEvent, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleEvents_ShouldDispatchAllEvents()
    {
        // Arrange
        var services = new ServiceCollection();
        var eventHandler = new Mock<IDomainEventHandler<TestDomainEvent>>();
        services.AddSingleton(eventHandler.Object);

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(serviceProvider, _loggerMock.Object);

        var event1 = new TestDomainEvent("Test Value 1");
        var event2 = new TestDomainEvent("Test Value 2");
        var events = new[] { event1, event2 };

        // Act
        await dispatcher.DispatchAsync(events);

        // Assert
        eventHandler.Verify(x => x.HandleAsync(event1, CancellationToken.None), Times.Once);
        eventHandler.Verify(x => x.HandleAsync(event2, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleHandlers_ShouldDispatchToAllHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler1 = new Mock<IDomainEventHandler<TestDomainEvent>>();
        var handler2 = new Mock<IDomainEventHandler<TestDomainEvent>>();
        services.AddSingleton(handler1.Object);
        services.AddSingleton(handler2.Object);

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(serviceProvider, _loggerMock.Object);

        var domainEvent = new TestDomainEvent("Test Value");
        var events = new[] { domainEvent };

        // Act
        await dispatcher.DispatchAsync(events);

        // Assert
        handler1.Verify(x => x.HandleAsync(domainEvent, CancellationToken.None), Times.Once);
        handler2.Verify(x => x.HandleAsync(domainEvent, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_WithNoHandlers_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(serviceProvider, _loggerMock.Object);

        var domainEvent = new TestDomainEvent("Test Value");
        var events = new[] { domainEvent };

        // Act & Assert
        var action = async () => await dispatcher.DispatchAsync(events);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DispatchAsync_WithEmptyEventList_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(serviceProvider, _loggerMock.Object);
        var events = Array.Empty<IDomainEvent>();

        // Act & Assert
        var action = async () => await dispatcher.DispatchAsync(events);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DispatchAsync_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(serviceProvider, _loggerMock.Object);
        IDomainEvent? nullEvent = null;
        var events = new[] { nullEvent! };

        // Act & Assert
        var action = async () => await dispatcher.DispatchAsync(events);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DispatchAsync_WithCancellationToken_ShouldPassTokenToHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        var eventHandler = new Mock<IDomainEventHandler<TestDomainEvent>>();
        services.AddSingleton(eventHandler.Object);

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(serviceProvider, _loggerMock.Object);

        var domainEvent = new TestDomainEvent("Test Value");
        var events = new[] { domainEvent };
        var cancellationToken = new CancellationToken();

        // Act
        await dispatcher.DispatchAsync(events, cancellationToken);

        // Assert
        eventHandler.Verify(x => x.HandleAsync(domainEvent, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_WhenHandlerThrowsException_ShouldPropagateException()
    {
        // Arrange
        var services = new ServiceCollection();
        var eventHandler = new Mock<IDomainEventHandler<TestDomainEvent>>();
        var exception = new InvalidOperationException("Handler exception");
        eventHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestDomainEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
        services.AddSingleton(eventHandler.Object);

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(serviceProvider, _loggerMock.Object);

        var domainEvent = new TestDomainEvent("Test Value");
        var events = new[] { domainEvent };

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.DispatchAsync(events)
        );

        thrownException.Should().Be(exception);
    }
}

// Test domain event for dispatcher tests
public class TestDomainEvent : DomainEvent
{
    public string Value { get; }

    public TestDomainEvent(string value)
    {
        Value = value;
    }
}

// Test domain event handler for dispatcher tests
public class TestDomainEventHandler : IDomainEventHandler<TestDomainEvent>
{
    public Task HandleAsync(
        TestDomainEvent domainEvent,
        CancellationToken cancellationToken = default
    )
    {
        return Task.CompletedTask;
    }
}
