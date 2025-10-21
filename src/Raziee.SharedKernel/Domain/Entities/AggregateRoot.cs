using Raziee.SharedKernel.Domain.Events;

namespace Raziee.SharedKernel.Domain.Entities;

/// <summary>
/// Base class for aggregate roots in the domain.
/// Aggregate roots are the entry points to aggregates and are responsible for maintaining consistency boundaries.
/// They can raise domain events that will be dispatched after the aggregate is saved.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root's identifier</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected AggregateRoot(TId id)
        : base(id) { }

    protected AggregateRoot()
    {
        // For EF Core
    }

    /// <summary>
    /// Gets the collection of domain events raised by this aggregate root.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the aggregate root.
    /// The event will be dispatched when the aggregate is saved.
    /// </summary>
    /// <param name="domainEvent">The domain event to add</param>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Removes a domain event from the aggregate root.
    /// </summary>
    /// <param name="domainEvent">The domain event to remove</param>
    protected void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Clears all domain events from the aggregate root.
    /// This method is typically called after the events have been dispatched.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Checks if the aggregate root has any domain events.
    /// </summary>
    /// <returns>True if there are domain events; otherwise, false</returns>
    public bool HasDomainEvents()
    {
        return _domainEvents.Count > 0;
    }
}
