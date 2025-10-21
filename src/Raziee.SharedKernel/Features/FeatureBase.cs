namespace Raziee.SharedKernel.Features;

/// <summary>
/// Base class for features in vertical slice architecture.
/// Provides common functionality for all features.
/// </summary>
public abstract class FeatureBase : IFeature
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureBase"/> class.
    /// </summary>
    /// <param name="name">The name of the feature</param>
    /// <param name="version">The version of the feature</param>
    /// <param name="description">The description of the feature</param>
    protected FeatureBase(string name, string version, string description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    /// <summary>
    /// Gets the name of the feature.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the version of the feature.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the description of the feature.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets a value indicating whether the feature is enabled.
    /// </summary>
    public virtual bool IsEnabled => true;

    /// <summary>
    /// Gets the dependencies of the feature.
    /// </summary>
    public virtual IEnumerable<string> Dependencies => Enumerable.Empty<string>();

    /// <summary>
    /// Initializes the feature.
    /// Override this method to perform feature-specific initialization.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public virtual Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Shuts down the feature.
    /// Override this method to perform feature-specific cleanup.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public virtual Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns a string representation of the feature.
    /// </summary>
    /// <returns>A string that represents the current feature</returns>
    public override string ToString()
    {
        return $"{Name} v{Version} - {Description}";
    }
}
