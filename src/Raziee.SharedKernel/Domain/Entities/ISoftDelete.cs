namespace Raziee.SharedKernel.Domain.Entities;

/// <summary>
/// Interface for entities that support soft deletion.
/// Soft deletion allows entities to be marked as deleted without physically removing them from the database.
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// Gets or sets a value indicating whether the entity has been soft deleted.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was soft deleted.
    /// </summary>
    DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who soft deleted the entity.
    /// </summary>
    string? DeletedBy { get; set; }
}
