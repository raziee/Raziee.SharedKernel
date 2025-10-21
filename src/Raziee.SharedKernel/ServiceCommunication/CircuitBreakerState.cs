namespace Raziee.SharedKernel.ServiceCommunication;

/// <summary>
/// Represents the state of a circuit breaker.
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// The circuit breaker is closed and allowing requests.
    /// </summary>
    Closed,

    /// <summary>
    /// The circuit breaker is open and blocking requests.
    /// </summary>
    Open,

    /// <summary>
    /// The circuit breaker is half-open and testing requests.
    /// </summary>
    HalfOpen,
}
