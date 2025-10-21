namespace Raziee.SharedKernel.Modules.Events;

/// <summary>
/// Interface for handling integration events.
/// Implement this interface to handle specific integration events.
/// </summary>
/// <typeparam name="TIntegrationEvent">The type of integration event to handle</typeparam>
public interface IIntegrationEventHandler<in TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Handles the specified integration event.
    /// </summary>
    /// <param name="integrationEvent">The integration event to handle</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleAsync(
        TIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    );
}
