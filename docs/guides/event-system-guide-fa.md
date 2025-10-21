# راهنمای سیستم رویداد

این راهنمای جامع سیستم رویداد در Raziee.SharedKernel را توضیح می‌دهد، شامل Domain Events، Integration Events، Inbox Pattern و Outbox Pattern.

## فهرست مطالب

- [مقدمه](#مقدمه)
- [معماری سیستم رویداد](#معماری-سیستم-رویداد)
- [Domain Events](#domain-events)
- [Integration Events](#integration-events)
- [Inbox Pattern](#inbox-pattern)
- [Outbox Pattern](#outbox-pattern)
- [مثال‌های جریان رویداد](#مثال‌های-جریان-رویداد)
- [بهترین شیوه‌ها](#بهترین-شیوه‌ها)
- [استراتژی‌های مهاجرت](#استراتژی‌های-مهاجرت)

## مقدمه

سیستم رویداد در Raziee.SharedKernel پایه محکمی برای ساخت معماری‌های event-driven فراهم می‌کند. این سیستم از طریق مجموعه جامعی از رابط‌ها و پیاده‌سازی‌ها، هم الگوهای modular monolith و هم microservices را پشتیبانی می‌کند.

### اجزای کلیدی

1. **Domain Events** - رویدادهای داخلی در یک bounded context
2. **Integration Events** - رویدادهای ارتباط cross-module
3. **Inbox Pattern** - پردازش پیام idempotent
4. **Outbox Pattern** - تحویل قابل اعتماد پیام

## معماری سیستم رویداد

```
┌──────────────────────────────────────────────────────────┐
│                    Event System Architecture             │
├──────────────────────────────────────────────────────────┤
│  ┌───────────────┐  ┌───────────────┐  ┌──────────────┐  │
│  │   Domain      │  │ Integration   │  │  Messaging   │  │
│  │   Events      │  │ Events        │  │  Patterns    │  │
│  │               │  │               │  │              │  │
│  │ • UserCreated │  │ • UserCreated │  │ • Inbox      │  │
│  │ • OrderPlaced │  │ • OrderPlaced │  │ • Outbox     │  │
│  │ • PaymentDone │  │ • PaymentDone │  │ • MessageBus │  │
│  └───────────────┘  └───────────────┘  └──────────────┘  │
│         │               │               │                │
│         └───────────────┼───────────────┘                │
│                         │                                │
│  ┌────────────────────────────────────────────────────┐  │
│  │              Event Processing Pipeline             │  │
│  │         (Domain → Integration → Messaging)         │  │
│  │                                                    │  │
│  │  Domain Events → Integration Events → Message Bus  │  │
│  └────────────────────────────────────────────────────┘  │
│                         │                                │
│  ┌────────────────────────────────────────────────────┐  │
│  │              Storage & Reliability                 │  │
│  │         (Inbox Store + Outbox Store)               │  │
│  │                                                    │  │
│  │  ┌─────────────┐  ┌─────────────┐                  │  │
│  │  │   Inbox     │  │   Outbox    │                  │  │
│  │  │   Store     │  │   Store     │                  │  │
│  │  │             │  │             │                  │  │
│  │  │ • Idempotent│  │ • Reliable  │                  │  │
│  │  │ • Dedupe    │  │ • Delivery  │                  │  │
│  │  └─────────────┘  └─────────────┘                  │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
```

## Domain Events

Domain events چیز مهمی را که در دامنه اتفاق افتاده نشان می‌دهند. آن‌ها برای ارتباط بین aggregate ها و تحریک عوارض جانبی در همان bounded context استفاده می‌شوند.

### تعریف رابط

```csharp
public interface IDomainEvent
{
    /// <summary>
    /// شناسه یکتای رویداد دامنه را دریافت می‌کند.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// تاریخ و زمان وقوع رویداد دامنه را دریافت می‌کند.
    /// </summary>
    DateTimeOffset OccurredOn { get; }

    /// <summary>
    /// نسخه aggregate که این رویداد را ایجاد کرده را دریافت می‌کند.
    /// این برای کنترل همزمانی خوشبینانه استفاده می‌شود.
    /// </summary>
    int Version { get; }
}
```

### پیاده‌سازی پایه

```csharp
public abstract class DomainEvent : IDomainEvent
{
    protected DomainEvent()
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
    }

    protected DomainEvent(int version)
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
        Version = version;
    }

    public Guid Id { get; }
    public DateTimeOffset OccurredOn { get; }
    public int Version { get; }
}
```

### مثال استفاده

```csharp
// تعریف Domain Event
public class UserCreatedEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string FirstName { get; }
    public string LastName { get; }

    public UserCreatedEvent(Guid userId, string email, string firstName, string lastName)
    {
        UserId = userId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }
}

// Domain Event Handler
public class UserCreatedDomainEventHandler : IDomainEventHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedDomainEventHandler> _logger;
    private readonly IOutboxStore _outboxStore;

    public UserCreatedDomainEventHandler(
        ILogger<UserCreatedDomainEventHandler> logger,
        IOutboxStore outboxStore)
    {
        _logger = logger;
        _outboxStore = outboxStore;
    }

    public async Task Handle(UserCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling UserCreatedEvent for user {UserId}", domainEvent.UserId);

        // منطق پردازش رویداد دامنه
        // مثلاً ارسال ایمیل خوشامدگویی
        await SendWelcomeEmailAsync(domainEvent.UserId, domainEvent.Email);

        // تبدیل به Integration Event
        var integrationEvent = new UserCreatedIntegrationEvent
        {
            UserId = domainEvent.UserId,
            Email = domainEvent.Email,
            FirstName = domainEvent.FirstName,
            LastName = domainEvent.LastName,
            OccurredOn = domainEvent.OccurredOn
        };

        // ذخیره در outbox برای تحویل قابل اعتماد
        await _outboxStore.StoreAsync(integrationEvent, cancellationToken);
    }

    private async Task SendWelcomeEmailAsync(Guid userId, string email)
    {
        // منطق ارسال ایمیل
        await Task.CompletedTask;
    }
}
```

### Domain Event Dispatcher

```csharp
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var events = domainEvents.ToList();
        if (!events.Any())
            return;

        _logger.LogDebug("Dispatching {Count} domain events", events.Count);

        foreach (var domainEvent in events)
        {
            await DispatchEventAsync(domainEvent, cancellationToken);
        }
    }

    private async Task DispatchEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

        var handlers = _serviceProvider.GetServices(handlerType);
        if (!handlers.Any())
        {
            _logger.LogWarning("No handlers found for domain event {EventType}", eventType.Name);
            return;
        }

        var tasks = handlers.Select(handler =>
        {
            var method = handler.GetType().GetMethod("Handle");
            return (Task)method!.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
        });

        await Task.WhenAll(tasks);
        _logger.LogDebug("Dispatched domain event {EventType} to {HandlerCount} handlers", 
            eventType.Name, handlers.Count());
    }
}
```

## Integration Events

Integration events برای ارتباط بین ماژول‌ها یا سرویس‌ها استفاده می‌شوند. آن‌ها رویدادهای cross-boundary هستند که باید به صورت قابل اعتماد تحویل داده شوند.

### تعریف رابط

```csharp
public interface IIntegrationEvent
{
    /// <summary>
    /// شناسه یکتای رویداد ادغام را دریافت می‌کند.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// تاریخ و زمان وقوع رویداد ادغام را دریافت می‌کند.
    /// </summary>
    DateTimeOffset OccurredOn { get; }

    /// <summary>
    /// نوع رویداد را دریافت می‌کند.
    /// </summary>
    string EventType { get; }
}
```

### پیاده‌سازی پایه

```csharp
public abstract class IntegrationEvent : IIntegrationEvent
{
    protected IntegrationEvent()
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }
    public DateTimeOffset OccurredOn { get; }
    public abstract string EventType { get; }
}
```

### مثال‌های Integration Events

```csharp
// User Module Events
public class UserCreatedIntegrationEvent : IntegrationEvent
{
    public override string EventType => "UserCreated";
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class UserUpdatedIntegrationEvent : IntegrationEvent
{
    public override string EventType => "UserUpdated";
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

// Product Module Events
public class ProductCreatedIntegrationEvent : IntegrationEvent
{
    public override string EventType => "ProductCreated";
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class ProductStockUpdatedIntegrationEvent : IntegrationEvent
{
    public override string EventType => "ProductStockUpdated";
    public Guid ProductId { get; set; }
    public int NewStock { get; set; }
    public int PreviousStock { get; set; }
}

// Order Module Events
public class OrderCreatedIntegrationEvent : IntegrationEvent
{
    public override string EventType => "OrderCreated";
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderStatusChangedIntegrationEvent : IntegrationEvent
{
    public override string EventType => "OrderStatusChanged";
    public Guid OrderId { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
}
```

### Integration Event Handler

```csharp
public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task Handle(TEvent @event, CancellationToken cancellationToken = default);
}

// User Module Event Handlers
public class UserCreatedIntegrationEventHandler : IIntegrationEventHandler<UserCreatedIntegrationEvent>
{
    private readonly ILogger<UserCreatedIntegrationEventHandler> _logger;

    public UserCreatedIntegrationEventHandler(ILogger<UserCreatedIntegrationEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(UserCreatedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling UserCreatedIntegrationEvent for user {UserId}", @event.UserId);

        // منطق پردازش رویداد
        // مثلاً به‌روزرسانی cache
        // یا ارسال notification

        await Task.CompletedTask;
    }
}

// Product Module Event Handlers
public class ProductStockUpdatedIntegrationEventHandler : IIntegrationEventHandler<ProductStockUpdatedIntegrationEvent>
{
    private readonly ILogger<ProductStockUpdatedIntegrationEventHandler> _logger;

    public ProductStockUpdatedIntegrationEventHandler(ILogger<ProductStockUpdatedIntegrationEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(ProductStockUpdatedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling ProductStockUpdatedIntegrationEvent for product {ProductId}", @event.ProductId);

        // منطق پردازش رویداد
        // مثلاً بررسی کمبود موجودی
        // یا به‌روزرسانی cache

        await Task.CompletedTask;
    }
}
```

## Inbox Pattern

Inbox pattern برای پردازش idempotent پیام‌ها استفاده می‌شود. این الگو تضمین می‌کند که هر پیام فقط یک بار پردازش شود.

### Inbox Store Interface

```csharp
public interface IInboxStore
{
    Task StoreAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
    Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IEnumerable<InboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default);
}

public class InboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
}
```

### پیاده‌سازی Inbox Store

```csharp
public class EfInboxStore : IInboxStore
{
    private readonly DbContext _context;
    private readonly ILogger<EfInboxStore> _logger;

    public EfInboxStore(DbContext context, ILogger<EfInboxStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task StoreAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        var message = new InboxMessage
        {
            Id = @event.Id,
            EventType = typeof(TEvent).Name,
            EventData = JsonSerializer.Serialize(@event),
            ReceivedAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        _context.Set<InboxMessage>().Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Stored inbox message {MessageId} for event {EventType}", message.Id, message.EventType);
    }

    public async Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<InboxMessage>()
            .AnyAsync(m => m.Id == eventId && m.ProcessedAt != null, cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var message = await _context.Set<InboxMessage>()
            .FirstOrDefaultAsync(m => m.Id == eventId, cancellationToken);

        if (message != null)
        {
            message.ProcessedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Marked inbox message {MessageId} as processed", eventId);
        }
    }

    public async Task<IEnumerable<InboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<InboxMessage>()
            .Where(m => m.ProcessedAt == null && m.RetryCount < 3)
            .OrderBy(m => m.ReceivedAt)
            .ToListAsync(cancellationToken);
    }
}
```

### Inbox Message Processor

```csharp
public class InboxMessageProcessor
{
    private readonly IInboxStore _inboxStore;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InboxMessageProcessor> _logger;

    public InboxMessageProcessor(
        IInboxStore inboxStore,
        IServiceProvider serviceProvider,
        ILogger<InboxMessageProcessor> logger)
    {
        _inboxStore = inboxStore;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ProcessMessagesAsync(CancellationToken cancellationToken = default)
    {
        var unprocessedMessages = await _inboxStore.GetUnprocessedMessagesAsync(cancellationToken);

        foreach (var message in unprocessedMessages)
        {
            try
            {
                await ProcessMessageAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing inbox message {MessageId}", message.Id);
                // منطق retry یا dead letter queue
            }
        }
    }

    private async Task ProcessMessageAsync(InboxMessage message, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing inbox message {MessageId} of type {EventType}", message.Id, message.EventType);

        // بررسی duplicate
        if (await _inboxStore.HasProcessedAsync(message.Id, cancellationToken))
        {
            _logger.LogDebug("Message {MessageId} already processed", message.Id);
            return;
        }

        // یافتن handler مناسب
        var eventType = Type.GetType(message.EventType);
        if (eventType == null)
        {
            _logger.LogWarning("Unknown event type {EventType}", message.EventType);
            return;
        }

        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        var handlers = _serviceProvider.GetServices(handlerType);

        if (!handlers.Any())
        {
            _logger.LogWarning("No handlers found for event type {EventType}", message.EventType);
            return;
        }

        // deserialize event
        var @event = JsonSerializer.Deserialize(message.EventData, eventType);

        // اجرای handlers
        var tasks = handlers.Select(handler =>
        {
            var method = handler.GetType().GetMethod("Handle");
            return (Task)method!.Invoke(handler, new object[] { @event, cancellationToken })!;
        });

        await Task.WhenAll(tasks);

        // علامت‌گذاری به عنوان پردازش شده
        await _inboxStore.MarkAsProcessedAsync(message.Id, cancellationToken);

        _logger.LogDebug("Successfully processed inbox message {MessageId}", message.Id);
    }
}
```

## Outbox Pattern

Outbox pattern برای تحویل قابل اعتماد پیام‌ها استفاده می‌شود. این الگو تضمین می‌کند که پیام‌ها در نهایت تحویل داده شوند.

### Outbox Store Interface

```csharp
public interface IOutboxStore
{
    Task StoreAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
    Task StoreAsync<TEvent>(TEvent @event, string topic, CancellationToken cancellationToken = default);
    Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}
```

### پیاده‌سازی Outbox Store

```csharp
public class EfOutboxStore : IOutboxStore
{
    private readonly DbContext _context;
    private readonly ILogger<EfOutboxStore> _logger;

    public EfOutboxStore(DbContext context, ILogger<EfOutboxStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task StoreAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        var message = new OutboxMessage
        {
            Id = @event.Id,
            EventType = typeof(TEvent).Name,
            EventData = JsonSerializer.Serialize(@event),
            CreatedAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        _context.Set<OutboxMessage>().Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Stored outbox message {MessageId} for event {EventType}", message.Id, message.EventType);
    }

    public async Task StoreAsync<TEvent>(TEvent @event, string topic, CancellationToken cancellationToken = default)
    {
        var message = new OutboxMessage
        {
            Id = @event.Id,
            EventType = typeof(TEvent).Name,
            EventData = JsonSerializer.Serialize(@event),
            Topic = topic,
            CreatedAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        _context.Set<OutboxMessage>().Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Stored outbox message {MessageId} for event {EventType} with topic {Topic}", 
            message.Id, message.EventType, topic);
    }

    public async Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null && m.RetryCount < 3)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.Set<OutboxMessage>()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message != null)
        {
            message.ProcessedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Marked outbox message {MessageId} as processed", messageId);
        }
    }

    public async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        var message = await _context.Set<OutboxMessage>()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message != null)
        {
            message.RetryCount++;
            message.Error = error;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Marked outbox message {MessageId} as failed: {Error}", messageId, error);
        }
    }
}
```

### Outbox Message Publisher

```csharp
public class OutboxMessagePublisher
{
    private readonly IOutboxStore _outboxStore;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<OutboxMessagePublisher> _logger;

    public OutboxMessagePublisher(
        IOutboxStore outboxStore,
        IMessageBus messageBus,
        ILogger<OutboxMessagePublisher> logger)
    {
        _outboxStore = outboxStore;
        _messageBus = messageBus;
        _logger = logger;
    }

    public async Task PublishPendingMessagesAsync(CancellationToken cancellationToken = default)
    {
        var pendingMessages = await _outboxStore.GetPendingMessagesAsync(cancellationToken);

        foreach (var message in pendingMessages)
        {
            try
            {
                await PublishMessageAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing outbox message {MessageId}", message.Id);
                await _outboxStore.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }
    }

    private async Task PublishMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Publishing outbox message {MessageId} of type {EventType}", message.Id, message.EventType);

        // deserialize event
        var eventType = Type.GetType(message.EventType);
        if (eventType == null)
        {
            _logger.LogWarning("Unknown event type {EventType}", message.EventType);
            return;
        }

        var @event = JsonSerializer.Deserialize(message.EventData, eventType);

        // انتشار به message bus
        if (!string.IsNullOrEmpty(message.Topic))
        {
            await _messageBus.PublishAsync(@event, message.Topic, cancellationToken);
        }
        else
        {
            await _messageBus.PublishAsync(@event, cancellationToken);
        }

        // علامت‌گذاری به عنوان پردازش شده
        await _outboxStore.MarkAsProcessedAsync(message.Id, cancellationToken);

        _logger.LogDebug("Successfully published outbox message {MessageId}", message.Id);
    }
}
```

## مثال‌های جریان رویداد

### 1. جریان کامل رویداد

```csharp
// 1. Domain Event در Aggregate
public class User : AggregateRoot<Guid>
{
    public string Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }

    public User(Guid id, string email, string firstName, string lastName) : base(id)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;

        // افزودن Domain Event
        AddDomainEvent(new UserCreatedEvent(id, email, firstName, lastName));
    }
}

// 2. Domain Event Handler
public class UserCreatedDomainEventHandler : IDomainEventHandler<UserCreatedEvent>
{
    private readonly IOutboxStore _outboxStore;
    private readonly ILogger<UserCreatedDomainEventHandler> _logger;

    public UserCreatedDomainEventHandler(
        IOutboxStore outboxStore,
        ILogger<UserCreatedDomainEventHandler> logger)
    {
        _outboxStore = outboxStore;
        _logger = logger;
    }

    public async Task Handle(UserCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling UserCreatedEvent for user {UserId}", domainEvent.UserId);

        // منطق پردازش داخلی
        await SendWelcomeEmailAsync(domainEvent.UserId, domainEvent.Email);

        // تبدیل به Integration Event
        var integrationEvent = new UserCreatedIntegrationEvent
        {
            UserId = domainEvent.UserId,
            Email = domainEvent.Email,
            FirstName = domainEvent.FirstName,
            LastName = domainEvent.LastName
        };

        // ذخیره در outbox
        await _outboxStore.StoreAsync(integrationEvent, cancellationToken);
    }

    private async Task SendWelcomeEmailAsync(Guid userId, string email)
    {
        // منطق ارسال ایمیل
        await Task.CompletedTask;
    }
}

// 3. Integration Event Handler
public class UserCreatedIntegrationEventHandler : IIntegrationEventHandler<UserCreatedIntegrationEvent>
{
    private readonly ILogger<UserCreatedIntegrationEventHandler> _logger;

    public UserCreatedIntegrationEventHandler(ILogger<UserCreatedIntegrationEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(UserCreatedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling UserCreatedIntegrationEvent for user {UserId}", @event.UserId);

        // منطق پردازش cross-module
        // مثلاً به‌روزرسانی cache
        // یا ارسال notification

        await Task.CompletedTask;
    }
}
```

### 2. پردازش Inbox Messages

```csharp
public class InboxMessageProcessingService : BackgroundService
{
    private readonly InboxMessageProcessor _processor;
    private readonly ILogger<InboxMessageProcessingService> _logger;

    public InboxMessageProcessingService(
        InboxMessageProcessor processor,
        ILogger<InboxMessageProcessingService> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting inbox message processing service");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _processor.ProcessMessagesAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in inbox message processing service");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("Stopped inbox message processing service");
    }
}
```

### 3. انتشار Outbox Messages

```csharp
public class OutboxMessagePublishingService : BackgroundService
{
    private readonly OutboxMessagePublisher _publisher;
    private readonly ILogger<OutboxMessagePublishingService> _logger;

    public OutboxMessagePublishingService(
        OutboxMessagePublisher publisher,
        ILogger<OutboxMessagePublishingService> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting outbox message publishing service");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _publisher.PublishPendingMessagesAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in outbox message publishing service");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("Stopped outbox message publishing service");
    }
}
```

## بهترین شیوه‌ها

### 1. طراحی رویدادها
- رویدادها را بر اساس وقایع کسب‌وکار طراحی کنید
- از نام‌های معنادار استفاده کنید
- versioning را در نظر بگیرید
- backward compatibility را حفظ کنید

### 2. مدیریت خطا
- retry policies را پیاده‌سازی کنید
- dead letter queues را استفاده کنید
- error handling مناسب پیاده‌سازی کنید
- monitoring و alerting را تنظیم کنید

### 3. عملکرد
- batch processing را استفاده کنید
- async processing را پیاده‌سازی کنید
- caching مناسب استفاده کنید
- resource management را در نظر بگیرید

### 4. امنیت
- message encryption را پیاده‌سازی کنید
- authentication و authorization را تنظیم کنید
- audit logging را پیاده‌سازی کنید
- data privacy را حفظ کنید

### 5. تست
- unit tests را بنویسید
- integration tests را پیاده‌سازی کنید
- end-to-end tests را انجام دهید
- performance tests را اجرا کنید

## استراتژی‌های مهاجرت

### 1. از Monolith به Modular Monolith

```csharp
public class MonolithToModularMigration
{
    private readonly ILogger<MonolithToModularMigration> _logger;

    public MonolithToModularMigration(ILogger<MonolithToModularMigration> logger)
    {
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting migration from monolith to modular monolith");

        // 1. شناسایی bounded contexts
        var boundedContexts = IdentifyBoundedContexts();

        // 2. استخراج domain events
        await ExtractDomainEventsAsync(boundedContexts, cancellationToken);

        // 3. پیاده‌سازی integration events
        await ImplementIntegrationEventsAsync(cancellationToken);

        // 4. تنظیم inbox/outbox patterns
        await SetupMessagingPatternsAsync(cancellationToken);

        _logger.LogInformation("Migration to modular monolith completed");
    }

    private List<string> IdentifyBoundedContexts()
    {
        return new List<string> { "User", "Product", "Order", "Payment" };
    }

    private async Task ExtractDomainEventsAsync(List<string> boundedContexts, CancellationToken cancellationToken)
    {
        foreach (var context in boundedContexts)
        {
            _logger.LogInformation("Extracting domain events for {Context}", context);
            // منطق استخراج domain events
            await Task.CompletedTask;
        }
    }

    private async Task ImplementIntegrationEventsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Implementing integration events");
        // منطق پیاده‌سازی integration events
        await Task.CompletedTask;
    }

    private async Task SetupMessagingPatternsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting up messaging patterns");
        // منطق تنظیم inbox/outbox patterns
        await Task.CompletedTask;
    }
}
```

### 2. از Modular Monolith به Microservices

```csharp
public class ModularToMicroservicesMigration
{
    private readonly ILogger<ModularToMicroservicesMigration> _logger;

    public ModularToMicroservicesMigration(ILogger<ModularToMicroservicesMigration> logger)
    {
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting migration from modular monolith to microservices");

        // 1. استخراج ماژول‌ها
        await ExtractModulesAsync(cancellationToken);

        // 2. پیاده‌سازی message bus
        await ImplementMessageBusAsync(cancellationToken);

        // 3. تنظیم service discovery
        await SetupServiceDiscoveryAsync(cancellationToken);

        // 4. پیاده‌سازی circuit breakers
        await ImplementCircuitBreakersAsync(cancellationToken);

        _logger.LogInformation("Migration to microservices completed");
    }

    private async Task ExtractModulesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Extracting modules to microservices");
        // منطق استخراج ماژول‌ها
        await Task.CompletedTask;
    }

    private async Task ImplementMessageBusAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Implementing message bus");
        // منطق پیاده‌سازی message bus
        await Task.CompletedTask;
    }

    private async Task SetupServiceDiscoveryAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting up service discovery");
        // منطق تنظیم service discovery
        await Task.CompletedTask;
    }

    private async Task ImplementCircuitBreakersAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Implementing circuit breakers");
        // منطق پیاده‌سازی circuit breakers
        await Task.CompletedTask;
    }
}
```

این راهنما پایه جامعی برای پیاده‌سازی سیستم رویداد با Raziee.SharedKernel ارائه می‌دهد، شامل تمام الگوها و شیوه‌های لازم برای ساخت سیستم‌های event-driven قابل اعتماد و مقیاس‌پذیر.
