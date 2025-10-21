namespace Raziee.SharedKernel.MultiTenancy;

/// <summary>
/// Interface for entities that belong to a tenant.
/// Used to implement multi-tenancy at the entity level.
/// </summary>
public interface ITenantEntity
{
    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    string TenantId { get; set; }
}
