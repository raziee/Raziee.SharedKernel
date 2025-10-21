using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Raziee.SharedKernel.Domain.Entities;

namespace Raziee.SharedKernel.Repositories;

/// <summary>
/// Generic repository implementation using Entity Framework Core.
/// Provides CRUD operations for entities with specification support.
/// </summary>
/// <typeparam name="TEntity">The type of entity</typeparam>
/// <typeparam name="TId">The type of the entity's identifier</typeparam>
public class EfRepository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    private readonly DbContext _context;
    private readonly DbSet<TEntity> _dbSet;
    private readonly ILogger<EfRepository<TEntity, TId>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger</param>
    public EfRepository(DbContext context, ILogger<EfRepository<TEntity, TId>> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<TEntity>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets an entity by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the entity</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The entity if found; otherwise, null</returns>
    public async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Getting entity of type {EntityType} with ID {Id}",
            typeof(TEntity).Name,
            id
        );
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <summary>
    /// Gets multiple entities by their identifiers.
    /// </summary>
    /// <param name="ids">The identifiers of the entities</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A collection of entities</returns>
    public async Task<IEnumerable<TEntity>> GetByIdsAsync(
        IEnumerable<TId> ids,
        CancellationToken cancellationToken = default
    )
    {
        var idList = ids.ToList();
        _logger.LogDebug(
            "Getting {Count} entities of type {EntityType} with IDs",
            idList.Count,
            typeof(TEntity).Name
        );
        return await _dbSet.Where(e => idList.Contains(e.Id)).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all entities.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A collection of all entities</returns>
    public async Task<IEnumerable<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("Getting all entities of type {EntityType}", typeof(TEntity).Name);
        return await _dbSet.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets entities with pagination.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based)</param>
    /// <param name="pageSize">The page size</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A paginated result containing entities and pagination information</returns>
    public async Task<PaginatedResult<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug(
            "Getting page {PageNumber} of size {PageSize} for entities of type {EntityType}",
            pageNumber,
            pageSize,
            typeof(TEntity).Name
        );

        var query = _dbSet.AsQueryable();
        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (pageNumber - 1) * pageSize;
        var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

        return new PaginatedResult<TEntity>(items, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Counts the total number of entities.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The total number of entities</returns>
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Counting entities of type {EntityType}", typeof(TEntity).Name);
        return await _dbSet.LongCountAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if an entity exists by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the entity</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>True if the entity exists; otherwise, false</returns>
    public async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Checking if entity of type {EntityType} with ID {Id} exists",
            typeof(TEntity).Name,
            id
        );
        return await _dbSet.AnyAsync(e => e.Id.Equals(id), cancellationToken);
    }

    /// <summary>
    /// Checks if any entities exist.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>True if any entities exist; otherwise, false</returns>
    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Checking if any entities of type {EntityType} exist",
            typeof(TEntity).Name
        );
        return await _dbSet.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _logger.LogDebug(
            "Adding entity of type {EntityType} with ID {Id}",
            typeof(TEntity).Name,
            entity.Id
        );
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Adds multiple entities to the repository.
    /// </summary>
    /// <param name="entities">The entities to add</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(entities);
        var entityList = entities.ToList();
        _logger.LogDebug(
            "Adding {Count} entities of type {EntityType}",
            entityList.Count,
            typeof(TEntity).Name
        );
        await _dbSet.AddRangeAsync(entityList, cancellationToken);
    }

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to update</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _logger.LogDebug(
            "Updating entity of type {EntityType} with ID {Id}",
            typeof(TEntity).Name,
            entity.Id
        );
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates multiple entities in the repository.
    /// </summary>
    /// <param name="entities">The entities to update</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task UpdateRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(entities);
        var entityList = entities.ToList();
        _logger.LogDebug(
            "Updating {Count} entities of type {EntityType}",
            entityList.Count,
            typeof(TEntity).Name
        );
        _dbSet.UpdateRange(entityList);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes an entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to remove</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _logger.LogDebug(
            "Deleting entity of type {EntityType} with ID {Id}",
            typeof(TEntity).Name,
            entity.Id
        );
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes multiple entities from the repository.
    /// </summary>
    /// <param name="entities">The entities to remove</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task DeleteRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(entities);
        var entityList = entities.ToList();
        _logger.LogDebug(
            "Deleting {Count} entities of type {EntityType}",
            entityList.Count,
            typeof(TEntity).Name
        );
        _dbSet.RemoveRange(entityList);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes an entity by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the entity to remove</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task DeleteByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Deleting entity of type {EntityType} with ID {Id}",
            typeof(TEntity).Name,
            id
        );
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
    }

    /// <summary>
    /// Removes multiple entities by their identifiers.
    /// </summary>
    /// <param name="ids">The identifiers of the entities to remove</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task DeleteByIdsAsync(
        IEnumerable<TId> ids,
        CancellationToken cancellationToken = default
    )
    {
        var idList = ids.ToList();
        _logger.LogDebug(
            "Deleting {Count} entities of type {EntityType} with IDs",
            idList.Count,
            typeof(TEntity).Name
        );
        var entities = await GetByIdsAsync(idList, cancellationToken);
        if (entities.Any())
        {
            _dbSet.RemoveRange(entities);
        }
    }
}
