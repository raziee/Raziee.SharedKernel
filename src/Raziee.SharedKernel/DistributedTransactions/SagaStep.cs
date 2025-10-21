namespace Raziee.SharedKernel.DistributedTransactions;

/// <summary>
/// Base class for saga steps.
/// Each step in a saga represents a single operation that can be executed and compensated.
/// </summary>
/// <typeparam name="TData">The type of saga data</typeparam>
public abstract class SagaStep<TData>
    where TData : class
{
    /// <summary>
    /// Gets the name of the saga step.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the description of the saga step.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Executes the saga step.
    /// </summary>
    /// <param name="data">The saga data</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public abstract Task ExecuteAsync(TData data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compensates for the saga step.
    /// This method is called when the saga needs to be rolled back.
    /// </summary>
    /// <param name="data">The saga data</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public abstract Task CompensateAsync(TData data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the step can be executed.
    /// </summary>
    /// <param name="data">The saga data</param>
    /// <returns>True if the step can be executed; otherwise, false</returns>
    public virtual bool CanExecute(TData data)
    {
        return true;
    }

    /// <summary>
    /// Determines whether the step can be compensated.
    /// </summary>
    /// <param name="data">The saga data</param>
    /// <returns>True if the step can be compensated; otherwise, false</returns>
    public virtual bool CanCompensate(TData data)
    {
        return true;
    }
}
