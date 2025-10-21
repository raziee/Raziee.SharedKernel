using MediatR;

namespace Raziee.SharedKernel.CQRS;

/// <summary>
/// Interface for handling queries.
/// This extends MediatR's IRequestHandler for proper integration.
/// </summary>
/// <typeparam name="TQuery">The type of query to handle</typeparam>
/// <typeparam name="TResponse">The type of the response</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse> { }
