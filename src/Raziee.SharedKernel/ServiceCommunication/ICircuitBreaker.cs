namespace Raziee.SharedKernel.ServiceCommunication;

/// <summary>
/// Interface for circuit breaker pattern.
/// Provides protection against cascading failures in distributed systems.
/// </summary>
public interface ICircuitBreaker
{
    /// <summary>
    /// Gets the name of the circuit breaker.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    CircuitBreakerState State { get; }

    /// <summary>
    /// Executes an operation through the circuit breaker.
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
    /// Executes an operation through the circuit breaker.
    /// </summary>
    /// <param name="operation">The operation to execute</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
