namespace Raziee.SharedKernel.Modules;

/// <summary>
/// Interface for modules in modular monolith architecture.
/// Modules represent self-contained business capabilities.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Gets the name of the module.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of the module.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the description of the module.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the dependencies of the module.
    /// </summary>
    IEnumerable<string> Dependencies { get; }

    /// <summary>
    /// Initializes the module.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuts down the module.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}
