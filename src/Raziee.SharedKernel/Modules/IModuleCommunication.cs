namespace Raziee.SharedKernel.Modules;

/// <summary>
/// Interface for inter-module communication.
/// Allows modules to communicate with each other through events and messages.
/// </summary>
public interface IModuleCommunication
{
    /// <summary>
    /// Publishes an event to all interested modules.
    /// </summary>
    /// <typeparam name="TEvent">The type of event</typeparam>
    /// <param name="event">The event to publish</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class;

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
        where TEvent : class;

    /// <summary>
    /// Sends a message to a specific module.
    /// </summary>
    /// <typeparam name="TMessage">The type of message</typeparam>
    /// <param name="targetModule">The target module name</param>
    /// <param name="message">The message to send</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SendAsync<TMessage>(
        string targetModule,
        TMessage message,
        CancellationToken cancellationToken = default
    )
        where TMessage : class;

    /// <summary>
    /// Registers a message handler for a specific module.
    /// </summary>
    /// <typeparam name="TMessage">The type of message</typeparam>
    /// <param name="handler">The message handler</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task RegisterHandlerAsync<TMessage>(
        Func<TMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default
    )
        where TMessage : class;
}
