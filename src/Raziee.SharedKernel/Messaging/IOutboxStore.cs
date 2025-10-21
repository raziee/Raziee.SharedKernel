namespace Raziee.SharedKernel.Messaging;

/// <summary>
/// Interface for storing outbox messages.
/// The outbox pattern ensures reliable message delivery by storing messages in the same database transaction.
/// </summary>
public interface IOutboxStore
{
    /// <summary>
    /// Stores an outbox message.
    /// </summary>
    /// <param name="message">The outbox message</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task StoreAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending outbox messages.
    /// </summary>
    /// <param name="batchSize">The batch size</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A collection of pending outbox messages</returns>
    Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Marks an outbox message as processed.
    /// </summary>
    /// <param name="messageId">The message ID</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an outbox message as failed.
    /// </summary>
    /// <param name="messageId">The message ID</param>
    /// <param name="error">The error message</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default
    );
}
