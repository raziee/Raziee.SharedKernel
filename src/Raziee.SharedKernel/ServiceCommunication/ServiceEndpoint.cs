namespace Raziee.SharedKernel.ServiceCommunication;

/// <summary>
/// Represents a service endpoint.
/// </summary>
public class ServiceEndpoint
{
    /// <summary>
    /// Gets or sets the host of the service endpoint.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the port of the service endpoint.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the protocol of the service endpoint.
    /// </summary>
    public string Protocol { get; set; } = "http";

    /// <summary>
    /// Gets or sets the path of the service endpoint.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the metadata of the service endpoint.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Gets the full URL of the service endpoint.
    /// </summary>
    public string Url => $"{Protocol}://{Host}:{Port}{Path}";
}
