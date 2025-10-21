namespace Raziee.SharedKernel.DistributedTransactions;

/// <summary>
/// Represents the status of a saga.
/// </summary>
public enum SagaStatus
{
    /// <summary>
    /// The saga is pending execution.
    /// </summary>
    Pending,

    /// <summary>
    /// The saga is currently executing.
    /// </summary>
    Running,

    /// <summary>
    /// The saga has completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// The saga has failed and is being compensated.
    /// </summary>
    Compensating,

    /// <summary>
    /// The saga has been fully compensated.
    /// </summary>
    Compensated,

    /// <summary>
    /// The saga has failed and cannot be compensated.
    /// </summary>
    Failed,
}
