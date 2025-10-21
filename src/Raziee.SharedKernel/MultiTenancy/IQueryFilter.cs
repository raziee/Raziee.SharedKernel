namespace Raziee.SharedKernel.MultiTenancy;

/// <summary>
/// Interface for query filters.
/// </summary>
/// <typeparam name="TEntity">The type of entity</typeparam>
public interface IQueryFilter<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Applies the filter to the query.
    /// </summary>
    /// <param name="query">The query to filter</param>
    /// <returns>The filtered query</returns>
    IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query);
}
