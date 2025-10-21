# راهنمای الگوهای Messaging

این راهنمای جامع نحوه استفاده از الگوهای messaging در Raziee.SharedKernel برای ارتباط غیرهمزمان قابل اعتماد در برنامه‌های .NET شما را نشان می‌دهد.

## فهرست مطالب

- [مقدمه](#مقدمه)
- [Message Bus](#message-bus)
- [Inbox Pattern](#inbox-pattern)
- [Outbox Pattern](#outbox-pattern)
- [Message Consumer/Publisher](#message-consumerpublisher)
- [مثال کامل: Messaging تجارت الکترونیک](#مثال-کامل-messaging-تجارت-الکترونیک)
- [بهترین شیوه‌ها](#بهترین-شیوه‌ها)

## مقدمه

الگوهای messaging ارتباط غیرهمزمان قابل اعتماد بین سرویس‌ها فراهم می‌کنند. Raziee.SharedKernel الگوهای Inbox/Outbox را پیاده‌سازی می‌کند تا تحویل پیام و جلوگیری از پردازش تکراری را تضمین کند.

## Message Bus

### 1. رابط Message Bus

```csharp
using Raziee.SharedKernel.Messaging;

public interface IMessageBus
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;
    
    Task SubscribeAsync<TMessage>(
        Func<TMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default
    )
        where TMessage : class;
    
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
```

### 2. پیاده‌سازی Message Bus

```csharp
public class RabbitMQMessageBus : IMessageBus
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMQMessageBus> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMQMessageBus(IConnectionFactory connectionFactory, ILogger<RabbitMQMessageBus> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _connection = _connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();
        
        // تنظیم exchanges و queues
        await ConfigureExchangesAsync();
        await ConfigureQueuesAsync();
        
        _logger.LogInformation("Message bus started successfully");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _channel?.Close();
        _connection?.Close();
        
        _logger.LogInformation("Message bus stopped");
        await Task.CompletedTask;
    }

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (_channel == null)
            throw new InvalidOperationException("Message bus is not started");

        var messageBody = JsonSerializer.SerializeToUtf8Bytes(message);
        var properties = _channel.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.MessageId = Guid.NewGuid().ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        var exchangeName = GetExchangeName<TMessage>();
        var routingKey = GetRoutingKey<TMessage>();

        _channel.BasicPublish(exchangeName, routingKey, properties, messageBody);
        
        _logger.LogDebug("Published message {MessageType} with ID {MessageId}", typeof(TMessage).Name, properties.MessageId);
        await Task.CompletedTask;
    }

    public async Task SubscribeAsync<TMessage>(
        Func<TMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default
    )
        where TMessage : class
    {
        if (_channel == null)
            throw new InvalidOperationException("Message bus is not started");

        var queueName = GetQueueName<TMessage>();
        var consumer = new EventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = JsonSerializer.Deserialize<TMessage>(body);
                
                if (message != null)
                {
                    await handler(message, cancellationToken);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {MessageType}", typeof(TMessage).Name);
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(queueName, false, consumer);
        
        _logger.LogInformation("Subscribed to messages of type {MessageType}", typeof(TMessage).Name);
        await Task.CompletedTask;
    }

    private async Task ConfigureExchangesAsync()
    {
        // تنظیم topic exchanges برای انواع مختلف پیام
        _channel.ExchangeDeclare("user.events", "topic", true);
        _channel.ExchangeDeclare("product.events", "topic", true);
        _channel.ExchangeDeclare("order.events", "topic", true);
        
        await Task.CompletedTask;
    }

    private async Task ConfigureQueuesAsync()
    {
        // تنظیم queues برای سرویس‌های مختلف
        _channel.QueueDeclare("user.service.queue", true, false, false);
        _channel.QueueDeclare("product.service.queue", true, false, false);
        _channel.QueueDeclare("order.service.queue", true, false, false);
        
        // Bind queues to exchanges
        _channel.QueueBind("user.service.queue", "user.events", "user.*");
        _channel.QueueBind("product.service.queue", "product.events", "product.*");
        _channel.QueueBind("order.service.queue", "order.events", "order.*");
        
        await Task.CompletedTask;
    }

    private string GetExchangeName<TMessage>()
    {
        var messageType = typeof(TMessage).Name;
        return messageType switch
        {
            var name when name.StartsWith("User") => "user.events",
            var name when name.StartsWith("Product") => "product.events",
            var name when name.StartsWith("Order") => "order.events",
            _ => "default.events"
        };
    }

    private string GetRoutingKey<TMessage>()
    {
        var messageType = typeof(TMessage).Name;
        return messageType.ToLower().Replace("event", "");
    }

    private string GetQueueName<TMessage>()
    {
        var messageType = typeof(TMessage).Name;
        return messageType switch
        {
            var name when name.StartsWith("User") => "user.service.queue",
            var name when name.StartsWith("Product") => "product.service.queue",
            var name when name.StartsWith("Order") => "order.service.queue",
            _ => "default.service.queue"
        };
    }
}
```

## Inbox Pattern

### 1. Inbox Store Interface

```csharp
public interface IInboxStore
{
    Task StoreAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;
    
    Task<bool> HasProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<InboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default);
}

public class InboxMessage
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string MessageData { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}
```

### 2. پیاده‌سازی Inbox Store

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

    public async Task StoreAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var inboxMessage = new InboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(TMessage).Name,
            MessageData = JsonSerializer.Serialize(message),
            ReceivedAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        _context.Set<InboxMessage>().Add(inboxMessage);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Stored inbox message {MessageId} of type {MessageType}", inboxMessage.Id, inboxMessage.MessageType);
    }

    public async Task<bool> HasProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<InboxMessage>()
            .AnyAsync(m => m.Id == messageId && m.ProcessedAt != null, cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.Set<InboxMessage>()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message != null)
        {
            message.ProcessedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Marked inbox message {MessageId} as processed", messageId);
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

### 3. Inbox Message Processor

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
                await HandleProcessingErrorAsync(message, ex, cancellationToken);
            }
        }
    }

    private async Task ProcessMessageAsync(InboxMessage message, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing inbox message {MessageId} of type {MessageType}", message.Id, message.MessageType);

        // بررسی duplicate
        if (await _inboxStore.HasProcessedAsync(message.Id, cancellationToken))
        {
            _logger.LogDebug("Message {MessageId} already processed", message.Id);
            return;
        }

        // یافتن handler مناسب
        var messageType = Type.GetType(message.MessageType);
        if (messageType == null)
        {
            _logger.LogWarning("Unknown message type {MessageType}", message.MessageType);
            return;
        }

        var handlerType = typeof(IMessageHandler<>).MakeGenericType(messageType);
        var handlers = _serviceProvider.GetServices(handlerType);

        if (!handlers.Any())
        {
            _logger.LogWarning("No handlers found for message type {MessageType}", message.MessageType);
            return;
        }

        // deserialize message
        var deserializedMessage = JsonSerializer.Deserialize(message.MessageData, messageType);

        // اجرای handlers
        var tasks = handlers.Select(handler =>
        {
            var method = handler.GetType().GetMethod("Handle");
            return (Task)method!.Invoke(handler, new object[] { deserializedMessage, cancellationToken })!;
        });

        await Task.WhenAll(tasks);

        // علامت‌گذاری به عنوان پردازش شده
        await _inboxStore.MarkAsProcessedAsync(message.Id, cancellationToken);

        _logger.LogDebug("Successfully processed inbox message {MessageId}", message.Id);
    }

    private async Task HandleProcessingErrorAsync(InboxMessage message, Exception exception, CancellationToken cancellationToken)
    {
        // افزایش تعداد retry
        message.RetryCount++;
        message.Error = exception.Message;

        if (message.RetryCount >= 3)
        {
            _logger.LogError("Message {MessageId} failed after {RetryCount} retries", message.Id, message.RetryCount);
            // ارسال به dead letter queue
        }
        else
        {
            _logger.LogWarning("Message {MessageId} will be retried (attempt {RetryCount})", message.Id, message.RetryCount);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

## Outbox Pattern

### 1. Outbox Store Interface

```csharp
public interface IOutboxStore
{
    Task StoreAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;
    
    Task StoreAsync<TMessage>(TMessage message, string topic, CancellationToken cancellationToken = default)
        where TMessage : class;
    
    Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(CancellationToken cancellationToken = default);
    
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string MessageData { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}
```

### 2. پیاده‌سازی Outbox Store

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

    public async Task StoreAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(TMessage).Name,
            MessageData = JsonSerializer.Serialize(message),
            CreatedAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        _context.Set<OutboxMessage>().Add(outboxMessage);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Stored outbox message {MessageId} of type {MessageType}", outboxMessage.Id, outboxMessage.MessageType);
    }

    public async Task StoreAsync<TMessage>(TMessage message, string topic, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(TMessage).Name,
            MessageData = JsonSerializer.Serialize(message),
            Topic = topic,
            CreatedAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        _context.Set<OutboxMessage>().Add(outboxMessage);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Stored outbox message {MessageId} of type {MessageType} with topic {Topic}", 
            outboxMessage.Id, outboxMessage.MessageType, topic);
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

### 3. Outbox Message Publisher

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
        _logger.LogDebug("Publishing outbox message {MessageId} of type {MessageType}", message.Id, message.MessageType);

        // deserialize message
        var messageType = Type.GetType(message.MessageType);
        if (messageType == null)
        {
            _logger.LogWarning("Unknown message type {MessageType}", message.MessageType);
            return;
        }

        var deserializedMessage = JsonSerializer.Deserialize(message.MessageData, messageType);

        // انتشار به message bus
        if (!string.IsNullOrEmpty(message.Topic))
        {
            await _messageBus.PublishAsync(deserializedMessage, message.Topic, cancellationToken);
        }
        else
        {
            await _messageBus.PublishAsync(deserializedMessage, cancellationToken);
        }

        // علامت‌گذاری به عنوان پردازش شده
        await _outboxStore.MarkAsProcessedAsync(message.Id, cancellationToken);

        _logger.LogDebug("Successfully published outbox message {MessageId}", message.Id);
    }
}
```

## Message Consumer/Publisher

### 1. Message Publisher

```csharp
public interface IMessagePublisher
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;
    
    Task PublishAsync<TMessage>(TMessage message, string topic, CancellationToken cancellationToken = default)
        where TMessage : class;
}

public class MessagePublisher : IMessagePublisher
{
    private readonly IOutboxStore _outboxStore;
    private readonly ILogger<MessagePublisher> _logger;

    public MessagePublisher(IOutboxStore outboxStore, ILogger<MessagePublisher> logger)
    {
        _outboxStore = outboxStore;
        _logger = logger;
    }

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        _logger.LogDebug("Publishing message {MessageType}", typeof(TMessage).Name);

        try
        {
            await _outboxStore.StoreAsync(message, cancellationToken);
            _logger.LogDebug("Message {MessageType} stored in outbox", typeof(TMessage).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store message {MessageType} in outbox", typeof(TMessage).Name);
            throw;
        }
    }

    public async Task PublishAsync<TMessage>(TMessage message, string topic, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        _logger.LogDebug("Publishing message {MessageType} to topic {Topic}", typeof(TMessage).Name, topic);

        try
        {
            await _outboxStore.StoreAsync(message, topic, cancellationToken);
            _logger.LogDebug("Message {MessageType} stored in outbox with topic {Topic}", typeof(TMessage).Name, topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store message {MessageType} in outbox with topic {Topic}", typeof(TMessage).Name, topic);
            throw;
        }
    }
}
```

### 2. Message Consumer

```csharp
public interface IMessageConsumer
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

public class MessageConsumer : IMessageConsumer
{
    private readonly IMessageBus _messageBus;
    private readonly IInboxStore _inboxStore;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessageConsumer> _logger;
    private readonly List<IDisposable> _subscriptions = new();

    public MessageConsumer(
        IMessageBus messageBus,
        IInboxStore inboxStore,
        IServiceProvider serviceProvider,
        ILogger<MessageConsumer> logger)
    {
        _messageBus = messageBus;
        _inboxStore = inboxStore;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting message consumer");

        // Subscribe to various message types
        await SubscribeToMessagesAsync(cancellationToken);

        _logger.LogInformation("Message consumer started successfully");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping message consumer");

        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
        _logger.LogInformation("Message consumer stopped successfully");
    }

    private async Task SubscribeToMessagesAsync(CancellationToken cancellationToken)
    {
        // Subscribe to user events
        await _messageBus.SubscribeAsync<UserCreatedEvent>(HandleUserCreatedEvent, cancellationToken);
        await _messageBus.SubscribeAsync<UserUpdatedEvent>(HandleUserUpdatedEvent, cancellationToken);

        // Subscribe to product events
        await _messageBus.SubscribeAsync<ProductCreatedEvent>(HandleProductCreatedEvent, cancellationToken);
        await _messageBus.SubscribeAsync<ProductUpdatedEvent>(HandleProductUpdatedEvent, cancellationToken);

        // Subscribe to order events
        await _messageBus.SubscribeAsync<OrderCreatedEvent>(HandleOrderCreatedEvent, cancellationToken);
        await _messageBus.SubscribeAsync<OrderStatusChangedEvent>(HandleOrderStatusChangedEvent, cancellationToken);
    }

    private async Task HandleUserCreatedEvent(UserCreatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling UserCreatedEvent for user {UserId}", @event.UserId);

        try
        {
            // ذخیره در inbox
            await _inboxStore.StoreAsync(@event, cancellationToken);

            // پردازش رویداد
            var handler = _serviceProvider.GetService<IMessageHandler<UserCreatedEvent>>();
            if (handler != null)
            {
                await handler.Handle(@event, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UserCreatedEvent for user {UserId}", @event.UserId);
            throw;
        }
    }

    private async Task HandleUserUpdatedEvent(UserUpdatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling UserUpdatedEvent for user {UserId}", @event.UserId);
        // منطق مشابه
        await Task.CompletedTask;
    }

    private async Task HandleProductCreatedEvent(ProductCreatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling ProductCreatedEvent for product {ProductId}", @event.ProductId);
        // منطق مشابه
        await Task.CompletedTask;
    }

    private async Task HandleProductUpdatedEvent(ProductUpdatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling ProductUpdatedEvent for product {ProductId}", @event.ProductId);
        // منطق مشابه
        await Task.CompletedTask;
    }

    private async Task HandleOrderCreatedEvent(OrderCreatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling OrderCreatedEvent for order {OrderId}", @event.OrderId);
        // منطق مشابه
        await Task.CompletedTask;
    }

    private async Task HandleOrderStatusChangedEvent(OrderStatusChangedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling OrderStatusChangedEvent for order {OrderId}", @event.OrderId);
        // منطق مشابه
        await Task.CompletedTask;
    }
}
```

## مثال کامل: Messaging تجارت الکترونیک

### 1. Message Handlers

```csharp
public interface IMessageHandler<in TMessage>
{
    Task Handle(TMessage message, CancellationToken cancellationToken = default);
}

// User Event Handlers
public class UserCreatedEventHandler : IMessageHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger;

    public UserCreatedEventHandler(ILogger<UserCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(UserCreatedEvent message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling UserCreatedEvent for user {UserId}", message.UserId);

        // منطق پردازش رویداد
        // مثلاً ارسال ایمیل خوشامدگویی
        // یا به‌روزرسانی cache

        await Task.CompletedTask;
    }
}

public class UserUpdatedEventHandler : IMessageHandler<UserUpdatedEvent>
{
    private readonly ILogger<UserUpdatedEventHandler> _logger;

    public UserUpdatedEventHandler(ILogger<UserUpdatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(UserUpdatedEvent message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling UserUpdatedEvent for user {UserId}", message.UserId);

        // منطق پردازش رویداد
        // مثلاً به‌روزرسانی cache
        // یا ارسال notification

        await Task.CompletedTask;
    }
}

// Product Event Handlers
public class ProductCreatedEventHandler : IMessageHandler<ProductCreatedEvent>
{
    private readonly ILogger<ProductCreatedEventHandler> _logger;

    public ProductCreatedEventHandler(ILogger<ProductCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(ProductCreatedEvent message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling ProductCreatedEvent for product {ProductId}", message.ProductId);

        // منطق پردازش رویداد
        // مثلاً به‌روزرسانی search index
        // یا ارسال notification

        await Task.CompletedTask;
    }
}

public class ProductUpdatedEventHandler : IMessageHandler<ProductUpdatedEvent>
{
    private readonly ILogger<ProductUpdatedEventHandler> _logger;

    public ProductUpdatedEventHandler(ILogger<ProductUpdatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(ProductUpdatedEvent message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling ProductUpdatedEvent for product {ProductId}", message.ProductId);

        // منطق پردازش رویداد
        // مثلاً به‌روزرسانی search index
        // یا به‌روزرسانی cache

        await Task.CompletedTask;
    }
}

// Order Event Handlers
public class OrderCreatedEventHandler : IMessageHandler<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedEventHandler> _logger;

    public OrderCreatedEventHandler(ILogger<OrderCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(OrderCreatedEvent message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling OrderCreatedEvent for order {OrderId}", message.OrderId);

        // منطق پردازش رویداد
        // مثلاً ارسال ایمیل تأیید
        // یا به‌روزرسانی موجودی محصولات

        await Task.CompletedTask;
    }
}

public class OrderStatusChangedEventHandler : IMessageHandler<OrderStatusChangedEvent>
{
    private readonly ILogger<OrderStatusChangedEventHandler> _logger;

    public OrderStatusChangedEventHandler(ILogger<OrderStatusChangedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(OrderStatusChangedEvent message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling OrderStatusChangedEvent for order {OrderId}", message.OrderId);

        // منطق پردازش رویداد
        // مثلاً ارسال notification
        // یا به‌روزرسانی status

        await Task.CompletedTask;
    }
}
```

### 2. Background Services

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

### 3. Service Registration

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // افزودن Raziee.SharedKernel
        builder.Services.AddSharedKernel();

        // افزودن Message Bus
        builder.Services.AddSingleton<IConnectionFactory>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("RabbitMQ");
            return new ConnectionFactory { Uri = new Uri(connectionString) };
        });

        builder.Services.AddSingleton<IMessageBus, RabbitMQMessageBus>();

        // افزودن Inbox/Outbox Stores
        builder.Services.AddScoped<IInboxStore, EfInboxStore>();
        builder.Services.AddScoped<IOutboxStore, EfOutboxStore>();

        // افزودن Message Publisher/Consumer
        builder.Services.AddScoped<IMessagePublisher, MessagePublisher>();
        builder.Services.AddScoped<IMessageConsumer, MessageConsumer>();

        // افزودن Message Processors
        builder.Services.AddScoped<InboxMessageProcessor>();
        builder.Services.AddScoped<OutboxMessagePublisher>();

        // افزودن Background Services
        builder.Services.AddHostedService<InboxMessageProcessingService>();
        builder.Services.AddHostedService<OutboxMessagePublishingService>();

        // افزودن Message Handlers
        builder.Services.AddScoped<IMessageHandler<UserCreatedEvent>, UserCreatedEventHandler>();
        builder.Services.AddScoped<IMessageHandler<UserUpdatedEvent>, UserUpdatedEventHandler>();
        builder.Services.AddScoped<IMessageHandler<ProductCreatedEvent>, ProductCreatedEventHandler>();
        builder.Services.AddScoped<IMessageHandler<ProductUpdatedEvent>, ProductUpdatedEventHandler>();
        builder.Services.AddScoped<IMessageHandler<OrderCreatedEvent>, OrderCreatedEventHandler>();
        builder.Services.AddScoped<IMessageHandler<OrderStatusChangedEvent>, OrderStatusChangedEventHandler>();

        var app = builder.Build();

        app.Run();
    }
}
```

## بهترین شیوه‌ها

### 1. طراحی پیام‌ها
- پیام‌ها را بر اساس رویدادهای کسب‌وکار طراحی کنید
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

این راهنما پایه جامعی برای پیاده‌سازی الگوهای messaging با Raziee.SharedKernel ارائه می‌دهد، شامل تمام الگوها و شیوه‌های لازم برای ساخت سیستم‌های messaging قابل اعتماد و مقیاس‌پذیر.
