namespace Raziee.SharedKernel.Domain.Exceptions;

/// <summary>
/// Exception thrown when a concurrency conflict occurs.
/// This exception is used to indicate that an entity has been modified by another process
/// since it was last read, causing a concurrency conflict.
/// </summary>
public class ConcurrencyException : DomainException
{
    /// <summary>
    /// Gets the type of the entity that had the concurrency conflict.
    /// </summary>
    public Type? EntityType { get; } = null;

    /// <summary>
    /// Gets the identifier of the entity that had the concurrency conflict.
    /// </summary>
    public object? EntityId { get; } = null;

    /// <summary>
    /// Gets the expected version of the entity.
    /// </summary>
    public int? ExpectedVersion { get; } = null;

    /// <summary>
    /// Gets the actual version of the entity.
    /// </summary>
    public int? ActualVersion { get; } = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
    /// </summary>
    public ConcurrencyException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public ConcurrencyException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class with entity information and version details.
    /// </summary>
    /// <param name="entityType">The type of the entity that had the concurrency conflict</param>
    /// <param name="entityId">The identifier of the entity that had the concurrency conflict</param>
    /// <param name="expectedVersion">The expected version of the entity</param>
    /// <param name="actualVersion">The actual version of the entity</param>
    public ConcurrencyException(
        Type entityType,
        object? entityId,
        int expectedVersion,
        int actualVersion
    )
        : base(
            $"Concurrency conflict detected for entity of type '{entityType.Name}' with ID '{entityId}'. Expected version: {expectedVersion}, Actual version: {actualVersion}"
        )
    {
        EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
        EntityId = entityId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}

/// <summary>
/// Generic exception thrown when a concurrency conflict occurs.
/// This exception is used to indicate that an entity has been modified by another process
/// since it was last read, causing a concurrency conflict.
/// </summary>
/// <typeparam name="TEntity">The type of the entity that had the concurrency conflict</typeparam>
public class ConcurrencyException<TEntity> : ConcurrencyException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException{TEntity}"/> class.
    /// </summary>
    public ConcurrencyException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException{TEntity}"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public ConcurrencyException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException{TEntity}"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException{TEntity}"/> class with entity identifier and version details.
    /// </summary>
    /// <param name="entityId">The identifier of the entity that had the concurrency conflict</param>
    /// <param name="expectedVersion">The expected version of the entity</param>
    /// <param name="actualVersion">The actual version of the entity</param>
    public ConcurrencyException(object? entityId, int expectedVersion, int actualVersion)
        : base(typeof(TEntity), entityId, expectedVersion, actualVersion) { }
}
