namespace Raziee.SharedKernel.Modules.Events;

/// <summary>
/// Marker interface for integration events.
/// Integration events are used for communication between modules in a modular monolith.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// Gets the unique identifier of the integration event.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the date and time when the integration event occurred.
    /// </summary>
    DateTimeOffset OccurredOn { get; }

    /// <summary>
    /// Gets the source module that raised the event.
    /// </summary>
    string SourceModule { get; }
}
