namespace Raziee.SharedKernel.ServiceCommunication;

/// <summary>
/// Interface for retry policies.
/// Provides configurable retry behavior for failed operations.
/// </summary>
public interface IRetryPolicy
{
    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// </summary>
    int MaxRetries { get; }

    /// <summary>
    /// Gets the base delay between retry attempts.
    /// </summary>
    TimeSpan BaseDelay { get; }

    /// <summary>
    /// Gets the maximum delay between retry attempts.
    /// </summary>
    TimeSpan MaxDelay { get; }

    /// <summary>
    /// Gets the backoff multiplier for exponential backoff.
    /// </summary>
    double BackoffMultiplier { get; }

    /// <summary>
    /// Executes an operation with retry logic.
    /// </summary>
    /// <typeparam name="TResult">The type of the result</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The result of the operation</returns>
    Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Executes an operation with retry logic.
    /// </summary>
    /// <param name="operation">The operation to execute</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether an exception should be retried.
    /// </summary>
    /// <param name="exception">The exception to check</param>
    /// <returns>True if the exception should be retried; otherwise, false</returns>
    bool ShouldRetry(Exception exception);
}
