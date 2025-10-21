using Raziee.SharedKernel.Domain.Entities;

namespace Raziee.SharedKernel.Repositories;

/// <summary>
/// Generic repository interface for entities.
/// Provides basic CRUD operations for entities.
/// </summary>
/// <typeparam name="TEntity">The type of entity</typeparam>
/// <typeparam name="TId">The type of the entity's identifier</typeparam>
public interface IRepository<TEntity, TId> : IReadRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities to the repository.
    /// </summary>
    /// <param name="entities">The entities to add</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to update</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple entities in the repository.
    /// </summary>
    /// <param name="entities">The entities to update</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task UpdateRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Removes an entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to remove</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple entities from the repository.
    /// </summary>
    /// <param name="entities">The entities to remove</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task DeleteRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Removes an entity by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the entity to remove</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task DeleteByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple entities by their identifiers.
    /// </summary>
    /// <param name="ids">The identifiers of the entities to remove</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task DeleteByIdsAsync(IEnumerable<TId> ids, CancellationToken cancellationToken = default);
}
