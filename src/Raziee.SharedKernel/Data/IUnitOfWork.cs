namespace Raziee.SharedKernel.Data;

/// <summary>
/// Interface for the Unit of Work pattern.
/// Provides a way to manage transactions and ensure data consistency.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Saves all changes made in this unit of work to the database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The number of state entries written to the database</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new transaction.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether there is an active transaction.
    /// </summary>
    bool HasActiveTransaction { get; }

    /// <summary>
    /// Gets a value indicating whether there are pending changes.
    /// </summary>
    bool HasPendingChanges { get; }
}
