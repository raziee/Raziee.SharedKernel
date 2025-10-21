namespace Raziee.SharedKernel.Domain.Events;

/// <summary>
/// Interface for handling domain events.
/// Implement this interface to handle specific domain events.
/// </summary>
/// <typeparam name="TDomainEvent">The type of domain event to handle</typeparam>
public interface IDomainEventHandler<in TDomainEvent> : IDomainEventHandler
    where TDomainEvent : IDomainEvent
{
    /// <summary>
    /// Handles the specified domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base interface for domain event handlers.
/// </summary>
public interface IDomainEventHandler { }
