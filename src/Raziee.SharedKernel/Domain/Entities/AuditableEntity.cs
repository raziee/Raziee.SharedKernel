namespace Raziee.SharedKernel.Domain.Entities;

/// <summary>
/// Base class for entities that need audit information.
/// Provides automatic tracking of creation and modification timestamps and user information.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier</typeparam>
public abstract class AuditableEntity<TId> : Entity<TId>
    where TId : notnull
{
    protected AuditableEntity(TId id) : base(id)
    {
    }

    protected AuditableEntity()
    {
        // For EF Core
    }

    /// <summary>
    /// Gets or sets the date and time when the entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who created the entity.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last updated the entity.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Marks the entity as updated with the current timestamp and user information.
    /// This method should be called whenever the entity is modified.
    /// </summary>
    /// <param name="updatedBy">The identifier of the user who updated the entity</param>
    public void MarkAsUpdated(string? updatedBy = null)
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Marks the entity as created with the current timestamp and user information.
    /// This method should be called when the entity is first created.
    /// </summary>
    /// <param name="createdBy">The identifier of the user who created the entity</param>
    public void MarkAsCreated(string? createdBy = null)
    {
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }
}
