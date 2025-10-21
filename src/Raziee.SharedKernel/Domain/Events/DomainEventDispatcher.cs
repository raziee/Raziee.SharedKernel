using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Raziee.SharedKernel.Domain.Events;

/// <summary>
/// Default implementation of the domain event dispatcher.
/// Uses the service provider to resolve and invoke event handlers.
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventDispatcher"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving handlers</param>
    /// <param name="logger">The logger for logging dispatch operations</param>
    public DomainEventDispatcher(
        IServiceProvider serviceProvider,
        ILogger<DomainEventDispatcher> logger
    )
    {
        _serviceProvider =
            serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Dispatches a collection of domain events.
    /// </summary>
    /// <param name="domainEvents">The domain events to dispatch</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        var events = domainEvents.ToList();
        if (!events.Any())
        {
            return;
        }

        _logger.LogDebug("Dispatching {Count} domain events", events.Count);

        var tasks = events.Select(domainEvent => DispatchAsync(domainEvent, cancellationToken));
        await Task.WhenAll(tasks);

        _logger.LogDebug("Successfully dispatched {Count} domain events", events.Count);
    }

    /// <summary>
    /// Dispatches a single domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to dispatch</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task DispatchAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        _logger.LogDebug(
            "Dispatching domain event {EventType} with ID {EventId}",
            domainEvent.GetType().Name,
            domainEvent.Id
        );

        try
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = _serviceProvider.GetServices(handlerType);

            var tasks = handlers
                .Cast<object>()
                .Select(handler =>
                {
                    var handleMethod = handler.GetType().GetMethod("HandleAsync");
                    if (handleMethod != null)
                    {
                        return (Task)
                            handleMethod.Invoke(
                                handler,
                                new object[] { domainEvent, cancellationToken }
                            )!;
                    }
                    return Task.CompletedTask;
                });

            await Task.WhenAll(tasks);

            _logger.LogDebug(
                "Successfully dispatched domain event {EventType} with ID {EventId}",
                domainEvent.GetType().Name,
                domainEvent.Id
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error dispatching domain event {EventType} with ID {EventId}",
                domainEvent.GetType().Name,
                domainEvent.Id
            );
            throw;
        }
    }
}
