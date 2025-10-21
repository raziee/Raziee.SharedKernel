using Microsoft.Extensions.Logging;

namespace Raziee.SharedKernel.ServiceCommunication;

/// <summary>
/// Default implementation of a retry policy.
/// Provides exponential backoff retry behavior.
/// </summary>
public class RetryPolicy : IRetryPolicy
{
    private readonly ILogger<RetryPolicy> _logger;
    private readonly Random _random = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryPolicy"/> class.
    /// </summary>
    /// <param name="maxRetries">The maximum number of retry attempts</param>
    /// <param name="baseDelay">The base delay between retry attempts</param>
    /// <param name="maxDelay">The maximum delay between retry attempts</param>
    /// <param name="backoffMultiplier">The backoff multiplier for exponential backoff</param>
    /// <param name="logger">The logger</param>
    public RetryPolicy(
        int maxRetries = 3,
        TimeSpan? baseDelay = null,
        TimeSpan? maxDelay = null,
        double backoffMultiplier = 2.0,
        ILogger<RetryPolicy>? logger = null
    )
    {
        MaxRetries = maxRetries;
        BaseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
        MaxDelay = maxDelay ?? TimeSpan.FromMinutes(1);
        BackoffMultiplier = backoffMultiplier;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; }

    /// <summary>
    /// Gets the base delay between retry attempts.
    /// </summary>
    public TimeSpan BaseDelay { get; }

    /// <summary>
    /// Gets the maximum delay between retry attempts.
    /// </summary>
    public TimeSpan MaxDelay { get; }

    /// <summary>
    /// Gets the backoff multiplier for exponential backoff.
    /// </summary>
    public double BackoffMultiplier { get; }

    /// <summary>
    /// Executes an operation with retry logic.
    /// </summary>
    /// <typeparam name="TResult">The type of the result</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default
    )
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= MaxRetries)
        {
            try
            {
                _logger.LogDebug(
                    "Executing operation (attempt {Attempt}/{MaxRetries})",
                    attempt + 1,
                    MaxRetries + 1
                );
                return await operation();
            }
            catch (Exception ex) when (ShouldRetry(ex))
            {
                lastException = ex;
                attempt++;

                if (attempt <= MaxRetries)
                {
                    var delay = CalculateDelay(attempt);
                    _logger.LogWarning(
                        ex,
                        "Operation failed (attempt {Attempt}/{MaxRetries}), retrying in {Delay}ms",
                        attempt,
                        MaxRetries + 1,
                        delay.TotalMilliseconds
                    );
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        _logger.LogError(lastException, "Operation failed after {MaxRetries} retries", MaxRetries);
        throw lastException!;
    }

    /// <summary>
    /// Executes an operation with retry logic.
    /// </summary>
    /// <param name="operation">The operation to execute</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task ExecuteAsync(
        Func<Task> operation,
        CancellationToken cancellationToken = default
    )
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= MaxRetries)
        {
            try
            {
                _logger.LogDebug(
                    "Executing operation (attempt {Attempt}/{MaxRetries})",
                    attempt + 1,
                    MaxRetries + 1
                );
                await operation();
                return;
            }
            catch (Exception ex) when (ShouldRetry(ex))
            {
                lastException = ex;
                attempt++;

                if (attempt <= MaxRetries)
                {
                    var delay = CalculateDelay(attempt);
                    _logger.LogWarning(
                        ex,
                        "Operation failed (attempt {Attempt}/{MaxRetries}), retrying in {Delay}ms",
                        attempt,
                        MaxRetries + 1,
                        delay.TotalMilliseconds
                    );
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        _logger.LogError(lastException, "Operation failed after {MaxRetries} retries", MaxRetries);
        throw lastException!;
    }

    /// <summary>
    /// Determines whether an exception should be retried.
    /// </summary>
    /// <param name="exception">The exception to check</param>
    /// <returns>True if the exception should be retried; otherwise, false</returns>
    public virtual bool ShouldRetry(Exception exception)
    {
        // Retry on transient exceptions
        return exception is TimeoutException
            || exception is HttpRequestException
            || exception is TaskCanceledException;
    }

    /// <summary>
    /// Calculates the delay for the next retry attempt.
    /// </summary>
    /// <param name="attempt">The current attempt number</param>
    /// <returns>The delay for the next retry attempt</returns>
    private TimeSpan CalculateDelay(int attempt)
    {
        var delay = TimeSpan.FromMilliseconds(
            BaseDelay.TotalMilliseconds * Math.Pow(BackoffMultiplier, attempt - 1)
        );

        // Add jitter to prevent thundering herd
        var jitter = TimeSpan.FromMilliseconds(_random.Next(0, 1000));
        delay = delay.Add(jitter);

        return delay > MaxDelay ? MaxDelay : delay;
    }
}
