using Microsoft.Extensions.Logging;
using Raziee.SharedKernel.ServiceCommunication;

namespace Raziee.SharedKernel.DependencyInjection;

/// <summary>
/// Default implementation of service discovery.
/// </summary>
public class DefaultServiceDiscovery : IServiceDiscovery
{
    private readonly ILogger<DefaultServiceDiscovery> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultServiceDiscovery"/> class.
    /// </summary>
    /// <param name="logger">The logger</param>
    public DefaultServiceDiscovery(ILogger<DefaultServiceDiscovery> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Discovers services by name.
    /// </summary>
    /// <param name="serviceName">The name of the service</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A collection of service endpoints</returns>
    public Task<IEnumerable<ServiceEndpoint>> DiscoverServicesAsync(
        string serviceName,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug(
            "Discovering services for {ServiceName} (default implementation)",
            serviceName
        );
        return Task.FromResult(Enumerable.Empty<ServiceEndpoint>());
    }

    /// <summary>
    /// Discovers a single service by name.
    /// </summary>
    /// <param name="serviceName">The name of the service</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A service endpoint if found; otherwise, null</returns>
    public Task<ServiceEndpoint?> DiscoverServiceAsync(
        string serviceName,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("Discovering service {ServiceName} (default implementation)", serviceName);
        return Task.FromResult<ServiceEndpoint?>(null);
    }

    /// <summary>
    /// Registers a service endpoint.
    /// </summary>
    /// <param name="serviceName">The name of the service</param>
    /// <param name="endpoint">The service endpoint</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task RegisterServiceAsync(
        string serviceName,
        ServiceEndpoint endpoint,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug(
            "Registering service {ServiceName} at {Endpoint} (default implementation)",
            serviceName,
            endpoint.Url
        );
        return Task.CompletedTask;
    }

    /// <summary>
    /// Unregisters a service endpoint.
    /// </summary>
    /// <param name="serviceName">The name of the service</param>
    /// <param name="endpoint">The service endpoint</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task UnregisterServiceAsync(
        string serviceName,
        ServiceEndpoint endpoint,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug(
            "Unregistering service {ServiceName} at {Endpoint} (default implementation)",
            serviceName,
            endpoint.Url
        );
        return Task.CompletedTask;
    }
}
