namespace Raziee.SharedKernel.Domain.Events;

/// <summary>
/// Base class for domain events.
/// Provides common properties and behavior for all domain events.
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> class.
    /// </summary>
    protected DomainEvent()
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> class with a specific version.
    /// </summary>
    /// <param name="version">The version of the aggregate that raised this event</param>
    protected DomainEvent(int version)
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
        Version = version;
    }

    /// <summary>
    /// Gets the unique identifier of the domain event.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the date and time when the domain event occurred.
    /// </summary>
    public DateTimeOffset OccurredOn { get; }

    /// <summary>
    /// Gets the version of the aggregate that raised this event.
    /// </summary>
    public int Version { get; }
}
