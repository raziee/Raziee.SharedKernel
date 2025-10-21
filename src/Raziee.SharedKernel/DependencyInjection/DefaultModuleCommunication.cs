using Microsoft.Extensions.Logging;
using Raziee.SharedKernel.Modules;

namespace Raziee.SharedKernel.DependencyInjection;

/// <summary>
/// Default implementation of module communication.
/// </summary>
public class DefaultModuleCommunication : IModuleCommunication
{
    private readonly ILogger<DefaultModuleCommunication> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultModuleCommunication"/> class.
    /// </summary>
    /// <param name="logger">The logger</param>
    public DefaultModuleCommunication(ILogger<DefaultModuleCommunication> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publishes an event to all interested modules.
    /// </summary>
    /// <typeparam name="TEvent">The type of event</typeparam>
    /// <param name="event">The event to publish</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        _logger.LogDebug(
            "Publishing event {EventType} (default implementation)",
            typeof(TEvent).Name
        );
        return Task.CompletedTask;
    }

    /// <summary>
    /// Subscribes to events of a specific type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event</typeparam>
    /// <param name="handler">The event handler</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task SubscribeAsync<TEvent>(
        Func<TEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default
    )
        where TEvent : class
    {
        _logger.LogDebug(
            "Subscribing to event {EventType} (default implementation)",
            typeof(TEvent).Name
        );
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sends a message to a specific module.
    /// </summary>
    /// <typeparam name="TMessage">The type of message</typeparam>
    /// <param name="targetModule">The target module name</param>
    /// <param name="message">The message to send</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task SendAsync<TMessage>(
        string targetModule,
        TMessage message,
        CancellationToken cancellationToken = default
    )
        where TMessage : class
    {
        _logger.LogDebug(
            "Sending message {MessageType} to module {TargetModule} (default implementation)",
            typeof(TMessage).Name,
            targetModule
        );
        return Task.CompletedTask;
    }

    /// <summary>
    /// Registers a message handler for a specific module.
    /// </summary>
    /// <typeparam name="TMessage">The type of message</typeparam>
    /// <param name="handler">The message handler</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task RegisterHandlerAsync<TMessage>(
        Func<TMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default
    )
        where TMessage : class
    {
        _logger.LogDebug(
            "Registering handler for message {MessageType} (default implementation)",
            typeof(TMessage).Name
        );
        return Task.CompletedTask;
    }
}
