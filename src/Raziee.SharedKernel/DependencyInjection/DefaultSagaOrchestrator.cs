using Microsoft.Extensions.Logging;
using Raziee.SharedKernel.DistributedTransactions;

namespace Raziee.SharedKernel.DependencyInjection;

/// <summary>
/// Default implementation of saga orchestrator.
/// </summary>
public class DefaultSagaOrchestrator : ISagaOrchestrator
{
    private readonly ILogger<DefaultSagaOrchestrator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultSagaOrchestrator"/> class.
    /// </summary>
    /// <param name="logger">The logger</param>
    public DefaultSagaOrchestrator(ILogger<DefaultSagaOrchestrator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Starts a new saga.
    /// </summary>
    /// <typeparam name="TData">The type of saga data</typeparam>
    /// <param name="sagaId">The unique identifier of the saga</param>
    /// <param name="data">The initial saga data</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task StartSagaAsync<TData>(
        Guid sagaId,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TData : class
    {
        _logger.LogDebug("Starting saga {SagaId} (default implementation)", sagaId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes the next step in the saga.
    /// </summary>
    /// <typeparam name="TData">The type of saga data</typeparam>
    /// <param name="sagaId">The unique identifier of the saga</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task ExecuteNextStepAsync<TData>(
        Guid sagaId,
        CancellationToken cancellationToken = default
    )
        where TData : class
    {
        _logger.LogDebug("Executing next step for saga {SagaId} (default implementation)", sagaId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Compensates for a failed saga step.
    /// </summary>
    /// <typeparam name="TData">The type of saga data</typeparam>
    /// <param name="sagaId">The unique identifier of the saga</param>
    /// <param name="stepIndex">The index of the step to compensate</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task CompensateStepAsync<TData>(
        Guid sagaId,
        int stepIndex,
        CancellationToken cancellationToken = default
    )
        where TData : class
    {
        _logger.LogDebug(
            "Compensating step {StepIndex} for saga {SagaId} (default implementation)",
            stepIndex,
            sagaId
        );
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current state of a saga.
    /// </summary>
    /// <typeparam name="TData">The type of saga data</typeparam>
    /// <param name="sagaId">The unique identifier of the saga</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The current saga state</returns>
    public Task<SagaState<TData>?> GetSagaStateAsync<TData>(
        Guid sagaId,
        CancellationToken cancellationToken = default
    )
        where TData : class
    {
        _logger.LogDebug("Getting state for saga {SagaId} (default implementation)", sagaId);
        return Task.FromResult<SagaState<TData>?>(null);
    }
}
