using MediatR;
using Microsoft.Extensions.Logging;
using Raziee.SharedKernel.Data;

namespace Raziee.SharedKernel.CQRS;

/// <summary>
/// Pipeline behavior for managing transactions.
/// This behavior automatically wraps requests in transactions.
/// </summary>
/// <typeparam name="TRequest">The type of request</typeparam>
/// <typeparam name="TResponse">The type of response</typeparam>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work</param>
    /// <param name="logger">The logger</param>
    public TransactionBehavior(
        IUnitOfWork unitOfWork,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger
    )
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the request by wrapping it in a transaction.
    /// </summary>
    /// <param name="request">The request to handle</param>
    /// <param name="next">The next handler in the pipeline</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The response from the next handler</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug(
            "Starting transaction for request of type {RequestType}",
            typeof(TRequest).Name
        );

        if (_unitOfWork.HasActiveTransaction)
        {
            _logger.LogDebug(
                "Transaction already active for request of type {RequestType}",
                typeof(TRequest).Name
            );
            return await next();
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogDebug(
                "Transaction committed successfully for request of type {RequestType}",
                typeof(TRequest).Name
            );
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing request of type {RequestType}, rolling back transaction",
                typeof(TRequest).Name
            );
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
