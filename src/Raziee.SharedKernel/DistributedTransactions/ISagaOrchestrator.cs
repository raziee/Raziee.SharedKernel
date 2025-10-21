namespace Raziee.SharedKernel.DistributedTransactions;

/// <summary>
/// Interface for orchestrating distributed transactions using the Saga pattern.
/// Sagas manage long-running business processes that span multiple services.
/// </summary>
public interface ISagaOrchestrator
{
    /// <summary>
    /// Starts a new saga.
    /// </summary>
    /// <typeparam name="TData">The type of saga data</typeparam>
    /// <param name="sagaId">The unique identifier of the saga</param>
    /// <param name="data">The initial saga data</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task StartSagaAsync<TData>(
        Guid sagaId,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TData : class;

    /// <summary>
    /// Executes the next step in the saga.
    /// </summary>
    /// <typeparam name="TData">The type of saga data</typeparam>
    /// <param name="sagaId">The unique identifier of the saga</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ExecuteNextStepAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default)
        where TData : class;

    /// <summary>
    /// Compensates for a failed saga step.
    /// </summary>
    /// <typeparam name="TData">The type of saga data</typeparam>
    /// <param name="sagaId">The unique identifier of the saga</param>
    /// <param name="stepIndex">The index of the step to compensate</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task CompensateStepAsync<TData>(
        Guid sagaId,
        int stepIndex,
        CancellationToken cancellationToken = default
    )
        where TData : class;

    /// <summary>
    /// Gets the current state of a saga.
    /// </summary>
    /// <typeparam name="TData">The type of saga data</typeparam>
    /// <param name="sagaId">The unique identifier of the saga</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The current saga state</returns>
    Task<SagaState<TData>?> GetSagaStateAsync<TData>(
        Guid sagaId,
        CancellationToken cancellationToken = default
    )
        where TData : class;
}
