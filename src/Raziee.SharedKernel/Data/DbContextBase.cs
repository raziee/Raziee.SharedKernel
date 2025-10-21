using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Raziee.SharedKernel.Domain.Entities;
using Raziee.SharedKernel.Domain.Events;

namespace Raziee.SharedKernel.Data;

/// <summary>
/// Base class for Entity Framework DbContext with domain event dispatching.
/// Automatically dispatches domain events when entities are saved.
/// </summary>
public abstract class DbContextBase : DbContext
{
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly ILogger<DbContextBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbContextBase"/> class.
    /// </summary>
    /// <param name="options">The options for this context</param>
    /// <param name="domainEventDispatcher">The domain event dispatcher</param>
    /// <param name="logger">The logger</param>
    protected DbContextBase(
        DbContextOptions options,
        IDomainEventDispatcher domainEventDispatcher,
        ILogger<DbContextBase> logger
    )
        : base(options)
    {
        _domainEventDispatcher =
            domainEventDispatcher ?? throw new ArgumentNullException(nameof(domainEventDispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// Automatically dispatches domain events after saving changes.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The number of state entries written to the database</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Saving changes to the database");

        try
        {
            // Dispatch domain events before saving changes
            await DispatchDomainEventsAsync(cancellationToken);

            // Save changes to the database
            var result = await base.SaveChangesAsync(cancellationToken);

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
    /// Dispatches domain events for all entities that have been modified.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var entitiesWithEvents = ChangeTracker
            .Entries()
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
}
