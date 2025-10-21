using Microsoft.Extensions.Logging;

namespace Raziee.SharedKernel.MultiTenancy;

/// <summary>
/// Query filter for automatically filtering entities by tenant.
/// This filter is applied to all queries to ensure tenant isolation.
/// </summary>
/// <typeparam name="TEntity">The type of entity</typeparam>
public class TenantQueryFilter<TEntity> : IQueryFilter<TEntity>
    where TEntity : class, ITenantEntity
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TenantQueryFilter<TEntity>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantQueryFilter{TEntity}"/> class.
    /// </summary>
    /// <param name="tenantProvider">The tenant provider</param>
    /// <param name="logger">The logger</param>
    public TenantQueryFilter(
        ITenantProvider tenantProvider,
        ILogger<TenantQueryFilter<TEntity>> logger
    )
    {
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Applies the tenant filter to the query.
    /// </summary>
    /// <param name="query">The query to filter</param>
    /// <returns>The filtered query</returns>
    public IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query)
    {
        var currentTenant = _tenantProvider.GetCurrentTenant();

        if (string.IsNullOrEmpty(currentTenant))
        {
            _logger.LogWarning(
                "No tenant context found for query on {EntityType}",
                typeof(TEntity).Name
            );
            return query;
        }

        _logger.LogDebug(
            "Applying tenant filter for tenant {TenantId} on {EntityType}",
            currentTenant,
            typeof(TEntity).Name
        );
        return query.Where(e => e.TenantId == currentTenant);
    }
}
