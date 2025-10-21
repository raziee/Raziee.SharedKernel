using MediatR;

namespace Raziee.SharedKernel.CQRS;

/// <summary>
/// Interface for pipeline behaviors.
/// Pipeline behaviors allow you to add cross-cutting concerns to request processing.
/// </summary>
/// <typeparam name="TRequest">The type of request</typeparam>
/// <typeparam name="TResponse">The type of response</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">The request to handle</param>
    /// <param name="next">The next handler in the pipeline</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The response</returns>
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    );
}

/// <summary>
/// Delegate for the next handler in the pipeline.
/// </summary>
/// <typeparam name="TResponse">The type of response</typeparam>
/// <returns>The response</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
