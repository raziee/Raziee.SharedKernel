using Raziee.SharedKernel.Domain.Entities;

namespace Raziee.SharedKernel.Repositories;

/// <summary>
/// Generic read-only repository interface for entities.
/// Provides read operations for entities.
/// </summary>
/// <typeparam name="TEntity">The type of entity</typeparam>
/// <typeparam name="TId">The type of the entity's identifier</typeparam>
public interface IReadRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    /// <summary>
    /// Gets an entity by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the entity</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The entity if found; otherwise, null</returns>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple entities by their identifiers.
    /// </summary>
    /// <param name="ids">The identifiers of the entities</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A collection of entities</returns>
    Task<IEnumerable<TEntity>> GetByIdsAsync(
        IEnumerable<TId> ids,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets all entities.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A collection of all entities</returns>
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with pagination.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based)</param>
    /// <param name="pageSize">The page size</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A paginated result containing entities and pagination information</returns>
    Task<PaginatedResult<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Counts the total number of entities.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The total number of entities</returns>
    Task<long> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an entity exists by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the entity</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>True if the entity exists; otherwise, false</returns>
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entities exist.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>True if any entities exist; otherwise, false</returns>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}
