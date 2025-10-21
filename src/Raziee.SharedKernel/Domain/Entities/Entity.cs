using System.ComponentModel.DataAnnotations;

namespace Raziee.SharedKernel.Domain.Entities;

/// <summary>
/// Base class for all entities in the domain.
/// Provides identity and equality comparison based on the entity's ID.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    [Key]
    public TId Id { get; protected set; } = default!;

    protected Entity(TId id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
    }

    protected Entity()
    {
        // For EF Core
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current entity.
    /// Two entities are considered equal if they have the same ID and type.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity</param>
    /// <returns>True if the objects are equal; otherwise, false</returns>
    public override bool Equals(object? obj)
    {
        return Equals(obj as Entity<TId>);
    }

    /// <summary>
    /// Determines whether the specified entity is equal to the current entity.
    /// Two entities are considered equal if they have the same ID and type.
    /// </summary>
    /// <param name="other">The entity to compare with the current entity</param>
    /// <returns>True if the entities are equal; otherwise, false</returns>
    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return Id.Equals(other.Id);
    }

    /// <summary>
    /// Returns the hash code for this entity.
    /// The hash code is based on the entity's ID and type.
    /// </summary>
    /// <returns>A hash code for the current entity</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    /// <summary>
    /// Determines whether two entities are equal.
    /// </summary>
    /// <param name="left">The first entity to compare</param>
    /// <param name="right">The second entity to compare</param>
    /// <returns>True if the entities are equal; otherwise, false</returns>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two entities are not equal.
    /// </summary>
    /// <param name="left">The first entity to compare</param>
    /// <param name="right">The second entity to compare</param>
    /// <returns>True if the entities are not equal; otherwise, false</returns>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    /// Returns a string representation of the entity.
    /// </summary>
    /// <returns>A string that represents the current entity</returns>
    public override string ToString()
    {
        return $"{GetType().Name}[Id={Id}]";
    }
}
