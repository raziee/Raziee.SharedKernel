namespace Raziee.SharedKernel.Modules.Events;

/// <summary>
/// Interface for dispatching integration events.
/// </summary>
public interface IIntegrationEventDispatcher
{
    /// <summary>
    /// Publishes an integration event to all registered handlers.
    /// </summary>
    /// <typeparam name="TIntegrationEvent">The type of integration event</typeparam>
    /// <param name="integrationEvent">The integration event to publish</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishAsync<TIntegrationEvent>(
        TIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
        where TIntegrationEvent : IIntegrationEvent;

    /// <summary>
    /// Subscribes to integration events of a specific type.
    /// </summary>
    /// <typeparam name="TIntegrationEvent">The type of integration event</typeparam>
    /// <param name="handler">The event handler</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SubscribeAsync<TIntegrationEvent>(
        Func<TIntegrationEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default
    )
        where TIntegrationEvent : IIntegrationEvent;
}
