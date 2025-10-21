using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Raziee.SharedKernel.Domain.Entities;
using Raziee.SharedKernel.Domain.Events;

namespace Raziee.SharedKernel.Data;

/// <summary>
/// Implementation of the Unit of Work pattern using Entity Framework Core.
/// Manages transactions and ensures data consistency.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly ILogger<UnitOfWork> _logger;
    private IDbContextTransaction? _transaction;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="domainEventDispatcher">The domain event dispatcher</param>
    /// <param name="logger">The logger</param>
    public UnitOfWork(
        DbContext context,
        IDomainEventDispatcher domainEventDispatcher,
        ILogger<UnitOfWork> logger
    )
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _domainEventDispatcher =
            domainEventDispatcher ?? throw new ArgumentNullException(nameof(domainEventDispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a value indicating whether there is an active transaction.
    /// </summary>
    public bool HasActiveTransaction => _transaction != null;

    /// <summary>
    /// Gets a value indicating whether there are pending changes.
    /// </summary>
    public bool HasPendingChanges => _context.ChangeTracker.HasChanges();

    /// <summary>
    /// Saves all changes made in this unit of work to the database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The number of state entries written to the database</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Saving changes to the database");

        try
        {
            // Dispatch domain events before saving changes
            await DispatchDomainEventsAsync(cancellationToken);

            // Save changes to the database
            var result = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Successfully saved {Count} changes to the database", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes to the database");
            throw;
        }
    }

    /// <summary>
    /// Begins a new transaction.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            _logger.LogWarning("A transaction is already active");
            return;
        }

        _logger.LogDebug("Beginning a new transaction");
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            _logger.LogWarning("No active transaction to commit");
            return;
        }

        try
        {
            _logger.LogDebug("Committing the current transaction");
            await _transaction.CommitAsync(cancellationToken);
            _logger.LogDebug("Successfully committed the transaction");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing the transaction");
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            _logger.LogWarning("No active transaction to rollback");
            return;
        }

        try
        {
            _logger.LogDebug("Rolling back the current transaction");
            await _transaction.RollbackAsync(cancellationToken);
            _logger.LogDebug("Successfully rolled back the transaction");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back the transaction");
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// Dispatches domain events for all entities that have been modified.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var entitiesWithEvents = _context
            .ChangeTracker.Entries()
            .Where(e => e.Entity is IAggregateRoot)
            .Select(e => (IAggregateRoot)e.Entity)
            .Where(e => e.HasDomainEvents())
            .ToList();

        var domainEvents = entitiesWithEvents.SelectMany(e => e.DomainEvents).ToList();

        // Clear domain events from entities
        entitiesWithEvents.ForEach(e => e.ClearDomainEvents());

        // Dispatch domain events
        if (domainEvents.Any())
        {
            _logger.LogDebug("Dispatching {Count} domain events", domainEvents.Count);
            await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
        }
    }

    /// <summary>
    /// Disposes the unit of work.
    /// </summary>
    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }

    /// <summary>
    /// Disposes the unit of work asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
        }
        await _context.DisposeAsync();
    }
}
