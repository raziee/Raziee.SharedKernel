namespace Raziee.SharedKernel.Messaging;

/// <summary>
/// Interface for storing inbox messages.
/// The inbox pattern ensures idempotent message processing by tracking processed messages.
/// </summary>
public interface IInboxStore
{
    /// <summary>
    /// Stores an inbox message.
    /// </summary>
    /// <param name="message">The inbox message</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task StoreAsync(IInboxStore message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a message has been processed.
    /// </summary>
    /// <param name="messageId">The message ID</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>True if the message has been processed; otherwise, false</returns>
    Task<bool> IsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as processed.
    /// </summary>
    /// <param name="messageId">The message ID</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
}
