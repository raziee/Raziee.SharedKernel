using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Raziee.SharedKernel.Domain.Entities;

namespace Raziee.SharedKernel.Data;

/// <summary>
/// Interceptor for automatically setting audit fields on auditable entities.
/// </summary>
public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditableEntityInterceptor"/> class.
    /// </summary>
    /// <param name="currentUserService">The current user service</param>
    public AuditableEntityInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService =
            currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <summary>
    /// Intercepts the saving of changes to set audit fields.
    /// </summary>
    /// <param name="eventData">The event data</param>
    /// <param name="result">The result</param>
    /// <returns>The result</returns>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        if (eventData.Context != null)
        {
            SetAuditFields(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Intercepts the saving of changes to set audit fields asynchronously.
    /// </summary>
    /// <param name="eventData">The event data</param>
    /// <param name="result">The result</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The result</returns>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Context != null)
        {
            SetAuditFields(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Sets audit fields on auditable entities.
    /// </summary>
    /// <param name="context">The database context</param>
    private void SetAuditFields(DbContext context)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var currentTime = DateTimeOffset.UtcNow;

        var entries = context.ChangeTracker.Entries<AuditableEntity<object>>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = currentTime;
                    entry.Entity.CreatedBy = currentUser;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = currentTime;
                    entry.Entity.UpdatedBy = currentUser;
                    break;
            }
        }
    }
}
