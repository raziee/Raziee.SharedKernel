using MediatR;

namespace Raziee.SharedKernel.CQRS;

/// <summary>
/// Marker interface for queries.
/// Queries represent a request for information and should not change the state of the system.
/// </summary>
/// <typeparam name="TResponse">The type of the response</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse> { }
