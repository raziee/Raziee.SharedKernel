namespace Raziee.SharedKernel.Messaging;

/// <summary>
/// Represents an outbox message.
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Gets or sets the unique identifier of the message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the type of the message.
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized message content.
    /// </summary>
    public string MessageContent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the message was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the message was processed.
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of processing attempts.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Gets or sets the error message if processing failed.
    /// </summary>
    public string? Error { get; set; }
}
