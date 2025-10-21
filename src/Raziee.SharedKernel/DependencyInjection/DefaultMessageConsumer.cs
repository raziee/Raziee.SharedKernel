using Microsoft.Extensions.Logging;
using Raziee.SharedKernel.Messaging;

namespace Raziee.SharedKernel.DependencyInjection;

/// <summary>
/// Default implementation of message consumer.
/// </summary>
public class DefaultMessageConsumer : IMessageConsumer
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<DefaultMessageConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMessageConsumer"/> class.
    /// </summary>
    /// <param name="messageBus">The message bus</param>
    /// <param name="logger">The logger</param>
    public DefaultMessageConsumer(IMessageBus messageBus, ILogger<DefaultMessageConsumer> logger)
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        return _messageBus.SubscribeAsync(handler, cancellationToken);
    }
}
