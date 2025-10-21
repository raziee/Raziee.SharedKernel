namespace Raziee.SharedKernel.DistributedTransactions;

/// <summary>
/// Represents the state of a saga.
/// </summary>
/// <typeparam name="TData">The type of saga data</typeparam>
public class SagaState<TData>
    where TData : class
{
    /// <summary>
    /// Gets or sets the unique identifier of the saga.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the current step index.
    /// </summary>
    public int CurrentStepIndex { get; set; }

    /// <summary>
    /// Gets or sets the saga data.
    /// </summary>
    public TData Data { get; set; } = default!;

    /// <summary>
    /// Gets or sets the status of the saga.
    /// </summary>
    public SagaStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the saga was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the saga was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the error message if the saga failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}
