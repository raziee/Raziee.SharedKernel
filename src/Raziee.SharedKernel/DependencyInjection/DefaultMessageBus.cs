using Microsoft.Extensions.Logging;
using Raziee.SharedKernel.Messaging;

namespace Raziee.SharedKernel.DependencyInjection;

/// <summary>
/// Default implementation of message bus.
/// </summary>
public class DefaultMessageBus : IMessageBus
{
    private readonly ILogger<DefaultMessageBus> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMessageBus"/> class.
    /// </summary>
    /// <param name="logger">The logger</param>
    public DefaultMessageBus(ILogger<DefaultMessageBus> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publishes a message to the message bus.
    /// </summary>
    /// <typeparam name="TMessage">The type of message</typeparam>
    /// <param name="message">The message to publish</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default
    )
        where TMessage : class
    {
        _logger.LogDebug(
            "Publishing message {MessageType} (default implementation)",
            typeof(TMessage).Name
        );
        return Task.CompletedTask;
    }

    /// <summary>
    /// Subscribes to messages of a specific type.
    /// </summary>
    /// <typeparam name="TMessage">The type of message</typeparam>
    /// <param name="handler">The message handler</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task SubscribeAsync<TMessage>(
        Func<TMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default
    )
        where TMessage : class
    {
        _logger.LogDebug(
            "Subscribing to message {MessageType} (default implementation)",
            typeof(TMessage).Name
        );
        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts the message bus.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting message bus (default implementation)");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the message bus.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stopping message bus (default implementation)");
        return Task.CompletedTask;
    }
}
