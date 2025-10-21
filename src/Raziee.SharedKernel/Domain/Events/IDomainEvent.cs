namespace Raziee.SharedKernel.Domain.Events;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something important that happened in the domain.
/// They are used to communicate between aggregates and trigger side effects.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the domain event.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the date and time when the domain event occurred.
    /// </summary>
    DateTimeOffset OccurredOn { get; }

    /// <summary>
    /// Gets the version of the aggregate that raised this event.
    /// This is used for optimistic concurrency control.
    /// </summary>
    int Version { get; }
}
