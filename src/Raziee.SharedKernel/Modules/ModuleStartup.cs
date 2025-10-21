using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Raziee.SharedKernel.Modules;

/// <summary>
/// Base class for module startup.
/// Provides common functionality for module initialization and configuration.
/// </summary>
public abstract class ModuleStartup
{
    /// <summary>
    /// Gets the name of the module.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the version of the module.
    /// </summary>
    public abstract string Version { get; }

    /// <summary>
    /// Gets the description of the module.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Gets the dependencies of the module.
    /// </summary>
    public virtual IEnumerable<string> Dependencies => Enumerable.Empty<string>();

    /// <summary>
    /// Configures the services for the module.
    /// </summary>
    /// <param name="services">The service collection</param>
    public abstract void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Configures the services for the module asynchronously.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public virtual Task ConfigureServicesAsync(
        IServiceCollection services,
        CancellationToken cancellationToken = default
    )
    {
        ConfigureServices(services);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Initializes the module.
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public virtual Task InitializeAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default
    )
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Shuts down the module.
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public virtual Task ShutdownAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default
    )
    {
        return Task.CompletedTask;
    }
}
