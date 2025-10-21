namespace Raziee.SharedKernel.Domain.ValueObjects;

/// <summary>
/// Base class for value objects in the domain.
/// Value objects are immutable objects that are defined by their attributes rather than their identity.
/// They use structural equality comparison based on their components.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Gets the components that define the equality of this value object.
    /// Override this method to specify which components should be used for equality comparison.
    /// </summary>
    /// <returns>An enumerable of objects that define the equality components</returns>
    protected abstract IEnumerable<object> GetEqualityComponents();

    /// <summary>
    /// Determines whether the specified object is equal to the current value object.
    /// Two value objects are considered equal if they have the same type and equality components.
    /// </summary>
    /// <param name="obj">The object to compare with the current value object</param>
    /// <returns>True if the objects are equal; otherwise, false</returns>
    public override bool Equals(object? obj)
    {
        return Equals(obj as ValueObject);
    }

    /// <summary>
    /// Determines whether the specified value object is equal to the current value object.
    /// Two value objects are considered equal if they have the same type and equality components.
    /// </summary>
    /// <param name="other">The value object to compare with the current value object</param>
    /// <returns>True if the value objects are equal; otherwise, false</returns>
    public bool Equals(ValueObject? other)
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

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Returns the hash code for this value object.
    /// The hash code is calculated based on the equality components.
    /// </summary>
    /// <returns>A hash code for the current value object</returns>
    public override int GetHashCode()
    {
        var components = GetEqualityComponents().ToArray();
        return components.Length switch
        {
            0 => 0,
            1 => components[0]?.GetHashCode() ?? 0,
            _ => HashCode.Combine(components),
        };
    }

    /// <summary>
    /// Determines whether two value objects are equal.
    /// </summary>
    /// <param name="left">The first value object to compare</param>
    /// <param name="right">The second value object to compare</param>
    /// <returns>True if the value objects are equal; otherwise, false</returns>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two value objects are not equal.
    /// </summary>
    /// <param name="left">The first value object to compare</param>
    /// <param name="right">The second value object to compare</param>
    /// <returns>True if the value objects are not equal; otherwise, false</returns>
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    /// Returns a string representation of the value object.
    /// </summary>
    /// <returns>A string that represents the current value object</returns>
    public override string ToString()
    {
        var components = GetEqualityComponents().Select(x => x?.ToString() ?? "null");
        return $"{GetType().Name}[{string.Join(", ", components)}]";
    }
}
