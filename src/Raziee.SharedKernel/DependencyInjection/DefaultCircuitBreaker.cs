using Microsoft.Extensions.Logging;
using Raziee.SharedKernel.ServiceCommunication;

namespace Raziee.SharedKernel.DependencyInjection;

/// <summary>
/// Default implementation of circuit breaker.
/// </summary>
public class DefaultCircuitBreaker : ICircuitBreaker
{
    private readonly ILogger<DefaultCircuitBreaker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultCircuitBreaker"/> class.
    /// </summary>
    /// <param name="logger">The logger</param>
    public DefaultCircuitBreaker(ILogger<DefaultCircuitBreaker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the name of the circuit breaker.
    /// </summary>
    public string Name => "Default";

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitBreakerState State => CircuitBreakerState.Closed;

    /// <summary>
    /// Executes an operation through the circuit breaker.
    /// </summary>
    /// <typeparam name="TResult">The type of the result</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The result of the operation</returns>
    public Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("Executing operation through circuit breaker (default implementation)");
        return operation();
    }

    /// <summary>
    /// Executes an operation through the circuit breaker.
    /// </summary>
    /// <param name="operation">The operation to execute</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing operation through circuit breaker (default implementation)");
        return operation();
    }
}
