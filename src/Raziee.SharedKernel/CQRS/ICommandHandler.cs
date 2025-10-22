using MediatR;

namespace Raziee.SharedKernel.CQRS;

/// <summary>
/// Interface for handling commands.
/// This extends MediatR's IRequestHandler for proper integration.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand { }

/// <summary>
/// Interface for handling commands that return a result.
/// This extends MediatR's IRequestHandler for proper integration.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle</typeparam>
/// <typeparam name="TResponse">The type of the response</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse> { }
