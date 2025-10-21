using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Raziee.SharedKernel.CQRS;

/// <summary>
/// Pipeline behavior for validating requests using FluentValidation.
/// This behavior automatically validates requests before they are processed.
/// </summary>
/// <typeparam name="TRequest">The type of request</typeparam>
/// <typeparam name="TResponse">The type of response</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validators">The validators for the request</param>
    /// <param name="logger">The logger</param>
    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger
    )
    {
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the request by validating it before processing.
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
        _logger.LogDebug("Validating request of type {RequestType}", typeof(TRequest).Name);

        if (!_validators.Any())
        {
            _logger.LogDebug(
                "No validators found for request of type {RequestType}",
                typeof(TRequest).Name
            );
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

        if (failures.Any())
        {
            _logger.LogWarning(
                "Validation failed for request of type {RequestType} with {FailureCount} failures",
                typeof(TRequest).Name,
                failures.Count
            );
            throw new ValidationException(failures);
        }

        _logger.LogDebug(
            "Validation successful for request of type {RequestType}",
            typeof(TRequest).Name
        );
        return await next();
    }
}
