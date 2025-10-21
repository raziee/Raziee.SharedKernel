namespace Raziee.SharedKernel.MultiTenancy;

/// <summary>
/// Interface for providing tenant information.
/// Used to get the current tenant context.
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// Gets the current tenant identifier.
    /// </summary>
    /// <returns>The current tenant identifier</returns>
    string? GetCurrentTenant();

    /// <summary>
    /// Gets a value indicating whether the current context is multi-tenant.
    /// </summary>
    /// <returns>True if the current context is multi-tenant; otherwise, false</returns>
    bool IsMultiTenant();
}
