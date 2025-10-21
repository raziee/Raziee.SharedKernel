namespace Raziee.SharedKernel.Modules.Events;

/// <summary>
/// Base class for integration events.
/// Provides common properties and behavior for all integration events.
/// </summary>
public abstract class IntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationEvent"/> class.
    /// </summary>
    /// <param name="sourceModule">The source module that raised the event</param>
    protected IntegrationEvent(string sourceModule)
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
        SourceModule = sourceModule ?? throw new ArgumentNullException(nameof(sourceModule));
    }

    /// <summary>
    /// Gets the unique identifier of the integration event.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the date and time when the integration event occurred.
    /// </summary>
    public DateTimeOffset OccurredOn { get; }

    /// <summary>
    /// Gets the source module that raised the event.
    /// </summary>
    public string SourceModule { get; }
}
