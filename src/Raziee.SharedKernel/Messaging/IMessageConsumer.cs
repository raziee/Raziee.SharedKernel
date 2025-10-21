namespace Raziee.SharedKernel.Messaging;

/// <summary>
/// Interface for consuming messages.
/// </summary>
public interface IMessageConsumer
{
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
}
