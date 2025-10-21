namespace Raziee.SharedKernel.Domain.Events;

/// <summary>
/// Interface for dispatching domain events.
/// Domain events are dispatched after the aggregate is saved to ensure consistency.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches a collection of domain events.
    /// </summary>
    /// <param name="domainEvents">The domain events to dispatch</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Dispatches a single domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to dispatch</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
