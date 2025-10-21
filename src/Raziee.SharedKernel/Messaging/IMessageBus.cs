namespace Raziee.SharedKernel.Messaging;

/// <summary>
/// Interface for a message bus.
/// Provides abstraction over different message brokers (RabbitMQ, Azure Service Bus, etc.).
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Publishes a message to the message bus.
    /// </summary>
    /// <typeparam name="TMessage">The type of message</typeparam>
    /// <param name="message">The message to publish</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Subscribes to messages of a specific type.
    /// </summary>
    /// <typeparam name="TMessage">The type of message</typeparam>
    /// <param name="handler">The message handler</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SubscribeAsync<TMessage>(
        Func<TMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default
    )
        where TMessage : class;

    /// <summary>
    /// Starts the message bus.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the message bus.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}
