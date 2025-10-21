using MediatR;

namespace Raziee.SharedKernel.CQRS;

/// <summary>
/// Marker interface for commands.
/// Commands represent an intention to change the state of the system.
/// </summary>
public interface ICommand : IRequest { }

/// <summary>
/// Marker interface for commands that return a result.
/// </summary>
/// <typeparam name="TResponse">The type of the response</typeparam>
public interface ICommand<out TResponse> : ICommand, IRequest<TResponse> { }
