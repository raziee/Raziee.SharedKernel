namespace Raziee.SharedKernel.Modules.Events;

/// <summary>
/// Interface for an event bus.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event to the event bus.
    /// </summary>
    /// <typeparam name="TEvent">The type of event</typeparam>
    /// <param name="event">The event to publish</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;

    /// <summary>
    /// Subscribes to events of a specific type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event</typeparam>
    /// <param name="handler">The event handler</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SubscribeAsync<TEvent>(
        Func<TEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default
    )
        where TEvent : IIntegrationEvent;
}
