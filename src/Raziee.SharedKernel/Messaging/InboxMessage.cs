namespace Raziee.SharedKernel.Messaging;

/// <summary>
/// Represents an inbox message.
/// </summary>
public class InboxMessage
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
    /// Gets or sets the date and time when the message was received.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the message was processed.
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; set; }
}
