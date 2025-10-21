namespace Raziee.SharedKernel.Domain.Exceptions;

/// <summary>
/// Exception thrown when an entity is not found.
/// This exception is used to indicate that a requested entity does not exist.
/// </summary>
public class EntityNotFoundException : DomainException
{
    /// <summary>
    /// Gets the type of the entity that was not found.
    /// </summary>
    public Type? EntityType { get; } = null;

    /// <summary>
    /// Gets the identifier of the entity that was not found.
    /// </summary>
    public object? EntityId { get; } = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
    /// </summary>
    public EntityNotFoundException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public EntityNotFoundException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public EntityNotFoundException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class with entity type and identifier.
    /// </summary>
    /// <param name="entityType">The type of the entity that was not found</param>
    /// <param name="entityId">The identifier of the entity that was not found</param>
    public EntityNotFoundException(Type entityType, object? entityId = null)
        : base($"Entity of type '{entityType.Name}' with ID '{entityId}' was not found.")
    {
        EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
        EntityId = entityId;
    }
}

/// <summary>
/// Generic exception thrown when an entity is not found.
/// This exception is used to indicate that a requested entity does not exist.
/// </summary>
/// <typeparam name="TEntity">The type of the entity that was not found</typeparam>
public class EntityNotFoundException<TEntity> : EntityNotFoundException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException{TEntity}"/> class.
    /// </summary>
    public EntityNotFoundException()
        : base(typeof(TEntity)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException{TEntity}"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public EntityNotFoundException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException{TEntity}"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public EntityNotFoundException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException{TEntity}"/> class with entity identifier.
    /// </summary>
    /// <param name="entityId">The identifier of the entity that was not found</param>
    public EntityNotFoundException(object? entityId)
        : base(typeof(TEntity), entityId) { }
}
