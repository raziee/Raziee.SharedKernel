namespace Raziee.SharedKernel.Domain.Exceptions;

/// <summary>
/// Base exception for domain-related errors.
/// All domain exceptions should inherit from this class.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    protected DomainException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    protected DomainException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    protected DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
