using Microsoft.Extensions.Logging;
using Raziee.SharedKernel.Messaging;

namespace Raziee.SharedKernel.DependencyInjection;

/// <summary>
/// Default implementation of message publisher.
/// </summary>
public class DefaultMessagePublisher : IMessagePublisher
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<DefaultMessagePublisher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMessagePublisher"/> class.
    /// </summary>
    /// <param name="messageBus">The message bus</param>
    /// <param name="logger">The logger</param>
    public DefaultMessagePublisher(IMessageBus messageBus, ILogger<DefaultMessagePublisher> logger)
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publishes a message.
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
        return _messageBus.PublishAsync(message, cancellationToken);
    }
}
