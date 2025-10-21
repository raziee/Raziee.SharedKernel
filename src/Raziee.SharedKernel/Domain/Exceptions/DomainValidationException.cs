namespace Raziee.SharedKernel.Domain.Exceptions;

/// <summary>
/// Exception thrown when domain validation fails.
/// This exception is used to indicate that business rules have been violated.
/// </summary>
public class DomainValidationException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainValidationException"/> class.
    /// </summary>
    public DomainValidationException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainValidationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public DomainValidationException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainValidationException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public DomainValidationException(string message, Exception innerException)
        : base(message, innerException) { }
}
