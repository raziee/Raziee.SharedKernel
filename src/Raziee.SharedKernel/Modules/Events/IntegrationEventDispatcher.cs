using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Raziee.SharedKernel.Modules.Events;

/// <summary>
/// Dispatcher for integration events.
/// Handles the publishing and subscription of integration events between modules.
/// </summary>
public class IntegrationEventDispatcher : IIntegrationEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IntegrationEventDispatcher> _logger;
    private readonly Dictionary<Type, List<Func<object, CancellationToken, Task>>> _handlers =
        new();

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationEventDispatcher"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="logger">The logger</param>
    public IntegrationEventDispatcher(
        IServiceProvider serviceProvider,
        ILogger<IntegrationEventDispatcher> logger
    )
    {
        _serviceProvider =
            serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publishes an integration event to all registered handlers.
    /// </summary>
    /// <typeparam name="TIntegrationEvent">The type of integration event</typeparam>
    /// <param name="integrationEvent">The integration event to publish</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task PublishAsync<TIntegrationEvent>(
        TIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
        where TIntegrationEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        _logger.LogDebug(
            "Publishing integration event {EventType} with ID {EventId} from module {SourceModule}",
            typeof(TIntegrationEvent).Name,
            integrationEvent.Id,
            integrationEvent.SourceModule
        );

        try
        {
            // Get all registered handlers for this event type
            var handlers = GetHandlers<TIntegrationEvent>();

            if (!handlers.Any())
            {
                _logger.LogDebug(
                    "No handlers registered for integration event {EventType}",
                    typeof(TIntegrationEvent).Name
                );
                return;
            }

            // Execute all handlers
            var tasks = handlers.Select(handler => handler(integrationEvent, cancellationToken));
            await Task.WhenAll(tasks);

            _logger.LogDebug(
                "Successfully published integration event {EventType} with ID {EventId}",
                typeof(TIntegrationEvent).Name,
                integrationEvent.Id
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error publishing integration event {EventType} with ID {EventId}",
                typeof(TIntegrationEvent).Name,
                integrationEvent.Id
            );
            throw;
        }
    }

    /// <summary>
    /// Subscribes to integration events of a specific type.
    /// </summary>
    /// <typeparam name="TIntegrationEvent">The type of integration event</typeparam>
    /// <param name="handler">The event handler</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task SubscribeAsync<TIntegrationEvent>(
        Func<TIntegrationEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default
    )
        where TIntegrationEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        _logger.LogDebug(
            "Subscribing to integration event {EventType}",
            typeof(TIntegrationEvent).Name
        );

        var eventType = typeof(TIntegrationEvent);
        if (!_handlers.ContainsKey(eventType))
        {
            _handlers[eventType] = new List<Func<object, CancellationToken, Task>>();
        }

        _handlers[eventType].Add((@event, ct) => handler((TIntegrationEvent)@event, ct));

        _logger.LogDebug(
            "Successfully subscribed to integration event {EventType}",
            typeof(TIntegrationEvent).Name
        );
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets all registered handlers for a specific integration event type.
    /// </summary>
    /// <typeparam name="TIntegrationEvent">The type of integration event</typeparam>
    /// <returns>A collection of handlers</returns>
    private IEnumerable<
        Func<TIntegrationEvent, CancellationToken, Task>
    > GetHandlers<TIntegrationEvent>()
        where TIntegrationEvent : IIntegrationEvent
    {
        var eventType = typeof(TIntegrationEvent);

        if (!_handlers.TryGetValue(eventType, out var handlers))
        {
            return Enumerable.Empty<Func<TIntegrationEvent, CancellationToken, Task>>();
        }

        return handlers.Select(handler =>
            (Func<TIntegrationEvent, CancellationToken, Task>)(
                (integrationEvent, cancellationToken) =>
                    handler(integrationEvent, cancellationToken)
            )
        );
    }
}
