using Raziee.SharedKernel.Domain.Events;

namespace Raziee.SharedKernel.Domain.Entities;

/// <summary>
/// Interface for aggregate roots that can raise domain events.
/// This interface allows the infrastructure layer to work with any aggregate root type.
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// Gets the collection of domain events raised by this aggregate root.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all domain events from the aggregate root.
    /// This method is typically called after the events have been dispatched.
    /// </summary>
    void ClearDomainEvents();

    /// <summary>
    /// Checks if the aggregate root has any domain events.
    /// </summary>
    /// <returns>True if there are domain events; otherwise, false</returns>
    bool HasDomainEvents();
}
