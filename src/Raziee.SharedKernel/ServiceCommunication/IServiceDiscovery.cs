namespace Raziee.SharedKernel.ServiceCommunication;

/// <summary>
/// Interface for service discovery.
/// Provides abstraction over different service discovery mechanisms.
/// </summary>
public interface IServiceDiscovery
{
    /// <summary>
    /// Discovers services by name.
    /// </summary>
    /// <param name="serviceName">The name of the service</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A collection of service endpoints</returns>
    Task<IEnumerable<ServiceEndpoint>> DiscoverServicesAsync(
        string serviceName,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Discovers a single service by name.
    /// </summary>
    /// <param name="serviceName">The name of the service</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A service endpoint if found; otherwise, null</returns>
    Task<ServiceEndpoint?> DiscoverServiceAsync(
        string serviceName,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Registers a service endpoint.
    /// </summary>
    /// <param name="serviceName">The name of the service</param>
    /// <param name="endpoint">The service endpoint</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task RegisterServiceAsync(
        string serviceName,
        ServiceEndpoint endpoint,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Unregisters a service endpoint.
    /// </summary>
    /// <param name="serviceName">The name of the service</param>
    /// <param name="endpoint">The service endpoint</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task UnregisterServiceAsync(
        string serviceName,
        ServiceEndpoint endpoint,
        CancellationToken cancellationToken = default
    );
}
