using Microsoft.Extensions.Logging;

namespace Raziee.SharedKernel.Modules.Events;

/// <summary>
/// In-memory implementation of an event bus for integration events.
/// This is suitable for modular monolith architectures where all modules run in the same process.
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly IIntegrationEventDispatcher _eventDispatcher;
    private readonly ILogger<InMemoryEventBus> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryEventBus"/> class.
    /// </summary>
    /// <param name="eventDispatcher">The integration event dispatcher</param>
    /// <param name="logger">The logger</param>
    public InMemoryEventBus(
        IIntegrationEventDispatcher eventDispatcher,
        ILogger<InMemoryEventBus> logger
    )
    {
        _eventDispatcher =
            eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publishes an event to the event bus.
    /// </summary>
    /// <typeparam name="TEvent">The type of event</typeparam>
    /// <param name="event">The event to publish</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default
    )
        where TEvent : IIntegrationEvent
    {
        _logger.LogDebug(
            "Publishing event {EventType} with ID {EventId} to in-memory event bus",
            typeof(TEvent).Name,
            @event.Id
        );

        await _eventDispatcher.PublishAsync(@event, cancellationToken);

        _logger.LogDebug(
            "Successfully published event {EventType} with ID {EventId} to in-memory event bus",
            typeof(TEvent).Name,
            @event.Id
        );
    }

    /// <summary>
    /// Subscribes to events of a specific type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event</typeparam>
    /// <param name="handler">The event handler</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task SubscribeAsync<TEvent>(
        Func<TEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default
    )
        where TEvent : IIntegrationEvent
    {
        _logger.LogDebug(
            "Subscribing to event {EventType} on in-memory event bus",
            typeof(TEvent).Name
        );

        await _eventDispatcher.SubscribeAsync(handler, cancellationToken);

        _logger.LogDebug(
            "Successfully subscribed to event {EventType} on in-memory event bus",
            typeof(TEvent).Name
        );
    }
}
