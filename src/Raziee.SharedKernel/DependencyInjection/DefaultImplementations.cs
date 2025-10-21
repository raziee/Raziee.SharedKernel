using Microsoft.Extensions.Logging;
using Raziee.SharedKernel.MultiTenancy;

namespace Raziee.SharedKernel.DependencyInjection;

/// <summary>
/// Default implementation of tenant provider.
/// </summary>
public class DefaultTenantProvider : ITenantProvider
{
    private readonly ILogger<DefaultTenantProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultTenantProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger</param>
    public DefaultTenantProvider(ILogger<DefaultTenantProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current tenant identifier.
    /// </summary>
    /// <returns>The current tenant identifier</returns>
    public string? GetCurrentTenant()
    {
        // This is a placeholder implementation
        // In a real application, this would get the tenant from the current context
        _logger.LogDebug("Getting current tenant (default implementation)");
        return null;
    }

    /// <summary>
    /// Gets a value indicating whether the current context is multi-tenant.
    /// </summary>
    /// <returns>True if the current context is multi-tenant; otherwise, false</returns>
    public bool IsMultiTenant()
    {
        return GetCurrentTenant() != null;
    }
}
