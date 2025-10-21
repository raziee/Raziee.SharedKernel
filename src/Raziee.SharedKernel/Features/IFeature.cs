namespace Raziee.SharedKernel.Features;

/// <summary>
/// Marker interface for features in vertical slice architecture.
/// Features represent self-contained business capabilities.
/// </summary>
public interface IFeature
{
    /// <summary>
    /// Gets the name of the feature.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of the feature.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the description of the feature.
    /// </summary>
    string Description { get; }
}
