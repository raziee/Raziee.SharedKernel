namespace Raziee.SharedKernel.Messaging;

/// <summary>
/// Interface for publishing messages.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message.
    /// </summary>
    /// <typeparam name="TMessage">The type of message</typeparam>
    /// <param name="message">The message to publish</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;
}
