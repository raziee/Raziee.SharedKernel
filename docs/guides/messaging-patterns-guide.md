# Messaging Patterns Guide

This comprehensive guide demonstrates how to use Raziee.SharedKernel messaging patterns for reliable asynchronous communication in your .NET applications.

## Table of Contents

- [Introduction](#introduction)
- [Message Bus](#message-bus)
- [Inbox Pattern](#inbox-pattern)
- [Outbox Pattern](#outbox-pattern)
- [Message Consumer/Publisher](#message-consumerpublisher)
- [Complete Example: E-Commerce Messaging](#complete-example-e-commerce-messaging)
- [Best Practices](#best-practices)

## Introduction

Messaging patterns provide reliable asynchronous communication between services. Raziee.SharedKernel implements the Inbox/Outbox patterns to ensure message delivery and prevent duplicate processing.

## Message Bus

### 1. Message Bus Interface

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

### 2. Message Bus Implementation

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
        
        // Configure exchanges and queues
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
        // Configure topic exchanges for different message types
        _channel.ExchangeDeclare("user.events", "topic", true);
        _channel.ExchangeDeclare("product.events", "topic", true);
        _channel.ExchangeDeclare("order.events", "topic", true);
        
        await Task.CompletedTask;
    }

    private async Task ConfigureQueuesAsync()
    {
        // Configure queues for different services
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
        return typeof(TMessage).Name switch
        {
            var name when name.Contains("User") => "user.events",
            var name when name.Contains("Product") => "product.events",
            var name when name.Contains("Order") => "order.events",
            _ => "default.events"
        };
    }

    private string GetRoutingKey<TMessage>()
    {
        return typeof(TMessage).Name.ToLowerInvariant();
    }

    private string GetQueueName<TMessage>()
    {
        return typeof(TMessage).Name switch
        {
            var name when name.Contains("User") => "user.service.queue",
            var name when name.Contains("Product") => "product.service.queue",
            var name when name.Contains("Order") => "order.service.queue",
            _ => "default.service.queue"
        };
    }
}
```

## Inbox Pattern

### 1. Inbox Store Interface

```csharp
using Raziee.SharedKernel.Messaging;

public interface IInboxStore
{
    Task StoreAsync(InboxMessage message, CancellationToken cancellationToken = default);
    Task<bool> IsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
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

### 2. Inbox Store Implementation

```csharp
public class DatabaseInboxStore : IInboxStore
{
    private readonly DbContext _context;
    private readonly ILogger<DatabaseInboxStore> _logger;

    public DatabaseInboxStore(DbContext context, ILogger<DatabaseInboxStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task StoreAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        _context.Set<InboxMessage>().Add(message);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogDebug("Stored inbox message {MessageId} of type {MessageType}", 
            message.Id, message.MessageType);
    }

    public async Task<bool> IsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.Set<InboxMessage>()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        
        return message?.ProcessedAt != null;
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
}
```

### 3. Inbox Message Handler

```csharp
public class InboxMessageHandler<TMessage> where TMessage : class
{
    private readonly IInboxStore _inboxStore;
    private readonly Func<TMessage, CancellationToken, Task> _handler;
    private readonly ILogger<InboxMessageHandler<TMessage>> _logger;

    public InboxMessageHandler(
        IInboxStore inboxStore,
        Func<TMessage, CancellationToken, Task> handler,
        ILogger<InboxMessageHandler<TMessage>> logger)
    {
        _inboxStore = inboxStore;
        _handler = handler;
        _logger = logger;
    }

    public async Task HandleAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        var messageId = GetMessageId(message);
        
        // Check if message has already been processed
        if (await _inboxStore.IsProcessedAsync(messageId, cancellationToken))
        {
            _logger.LogDebug("Message {MessageId} has already been processed, skipping", messageId);
            return;
        }

        try
        {
            // Store message in inbox
            var inboxMessage = new InboxMessage
            {
                Id = messageId,
                MessageType = typeof(TMessage).Name,
                MessageData = JsonSerializer.Serialize(message),
                ReceivedAt = DateTimeOffset.UtcNow
            };

            await _inboxStore.StoreAsync(inboxMessage, cancellationToken);

            // Process the message
            await _handler(message, cancellationToken);

            // Mark as processed
            await _inboxStore.MarkAsProcessedAsync(messageId, cancellationToken);
            
            _logger.LogInformation("Successfully processed message {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId}", messageId);
            throw;
        }
    }

    private Guid GetMessageId(TMessage message)
    {
        // Extract message ID from message properties
        // This would depend on your message structure
        var idProperty = typeof(TMessage).GetProperty("Id");
        if (idProperty?.GetValue(message) is Guid id)
        {
            return id;
        }
        
        // Fallback to generating a new ID
        return Guid.NewGuid();
    }
}
```

## Outbox Pattern

### 1. Outbox Store Interface

```csharp
using Raziee.SharedKernel.Messaging;

public interface IOutboxStore
{
    Task StoreAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default
    );
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string MessageData { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}
```

### 2. Outbox Store Implementation

```csharp
public class DatabaseOutboxStore : IOutboxStore
{
    private readonly DbContext _context;
    private readonly ILogger<DatabaseOutboxStore> _logger;

    public DatabaseOutboxStore(DbContext context, ILogger<DatabaseOutboxStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task StoreAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _context.Set<OutboxMessage>().Add(message);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogDebug("Stored outbox message {MessageId} of type {MessageType}", 
            message.Id, message.MessageType);
    }

    public async Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
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
            message.Error = error;
            message.RetryCount++;
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogWarning("Marked outbox message {MessageId} as failed: {Error}", messageId, error);
        }
    }
}
```

### 3. Outbox Message Processor

```csharp
public class OutboxMessageProcessor
{
    private readonly IOutboxStore _outboxStore;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<OutboxMessageProcessor> _logger;
    private readonly Timer _timer;

    public OutboxMessageProcessor(
        IOutboxStore outboxStore,
        IMessageBus messageBus,
        ILogger<OutboxMessageProcessor> logger)
    {
        _outboxStore = outboxStore;
        _messageBus = messageBus;
        _logger = logger;
        
        // Process messages every 5 seconds
        _timer = new Timer(ProcessMessages, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    private async void ProcessMessages(object? state)
    {
        try
        {
            var pendingMessages = await _outboxStore.GetPendingMessagesAsync(100);
            
            foreach (var message in pendingMessages)
            {
                await ProcessMessageAsync(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing outbox messages");
        }
    }

    private async Task ProcessMessageAsync(OutboxMessage message)
    {
        try
        {
            _logger.LogDebug("Processing outbox message {MessageId} of type {MessageType}", 
                message.Id, message.MessageType);

            // Deserialize and publish message
            var messageType = Type.GetType(message.MessageType);
            if (messageType == null)
            {
                _logger.LogWarning("Unknown message type {MessageType} for message {MessageId}", 
                    message.MessageType, message.Id);
                await _outboxStore.MarkAsFailedAsync(message.Id, "Unknown message type");
                return;
            }

            var messageData = JsonSerializer.Deserialize(message.MessageData, messageType);
            if (messageData == null)
            {
                _logger.LogWarning("Failed to deserialize message {MessageId}", message.Id);
                await _outboxStore.MarkAsFailedAsync(message.Id, "Failed to deserialize message");
                return;
            }

            // Publish message using reflection
            var publishMethod = typeof(IMessageBus)
                .GetMethod(nameof(IMessageBus.PublishAsync))
                ?.MakeGenericMethod(messageType);
            
            if (publishMethod != null)
            {
                await (Task)publishMethod.Invoke(_messageBus, new object[] { messageData, CancellationToken.None });
                await _outboxStore.MarkAsProcessedAsync(message.Id);
                
                _logger.LogInformation("Successfully processed outbox message {MessageId}", message.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing outbox message {MessageId}", message.Id);
            await _outboxStore.MarkAsFailedAsync(message.Id, ex.Message);
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
```

## Message Consumer/Publisher

### 1. Message Publisher

```csharp
using Raziee.SharedKernel.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;
}

public class OutboxMessagePublisher : IMessagePublisher
{
    private readonly IOutboxStore _outboxStore;
    private readonly ILogger<OutboxMessagePublisher> _logger;

    public OutboxMessagePublisher(IOutboxStore outboxStore, ILogger<OutboxMessagePublisher> logger)
    {
        _outboxStore = outboxStore;
        _logger = logger;
    }

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(TMessage).Name,
            MessageData = JsonSerializer.Serialize(message),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _outboxStore.StoreAsync(outboxMessage, cancellationToken);
        
        _logger.LogDebug("Stored message {MessageId} of type {MessageType} in outbox", 
            outboxMessage.Id, outboxMessage.MessageType);
    }
}
```

### 2. Message Consumer

```csharp
using Raziee.SharedKernel.Messaging;

public interface IMessageConsumer
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

public class MessageConsumer<TMessage> : IMessageConsumer where TMessage : class
{
    private readonly IMessageBus _messageBus;
    private readonly IInboxStore _inboxStore;
    private readonly Func<TMessage, CancellationToken, Task> _handler;
    private readonly ILogger<MessageConsumer<TMessage>> _logger;
    private readonly InboxMessageHandler<TMessage> _inboxHandler;

    public MessageConsumer(
        IMessageBus messageBus,
        IInboxStore inboxStore,
        Func<TMessage, CancellationToken, Task> handler,
        ILogger<MessageConsumer<TMessage>> logger)
    {
        _messageBus = messageBus;
        _inboxStore = inboxStore;
        _handler = handler;
        _logger = logger;
        _inboxHandler = new InboxMessageHandler<TMessage>(_inboxStore, _handler, 
            logger.CreateLogger<InboxMessageHandler<TMessage>>());
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting message consumer for {MessageType}", typeof(TMessage).Name);
        
        await _messageBus.SubscribeAsync<TMessage>(async (message, ct) =>
        {
            await _inboxHandler.HandleAsync(message, ct);
        }, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping message consumer for {MessageType}", typeof(TMessage).Name);
        await Task.CompletedTask;
    }
}
```

## Complete Example: E-Commerce Messaging

### 1. Domain Events

```csharp
public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class OrderCancelledEvent
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CancelledAt { get; set; }
}

public class PaymentProcessedEvent
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; }
}
```

### 2. Event Handlers

```csharp
public class OrderCreatedEventHandler
{
    private readonly ILogger<OrderCreatedEventHandler> _logger;

    public OrderCreatedEventHandler(ILogger<OrderCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(OrderCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing order created event for order {OrderId}", @event.OrderId);
        
        // Send confirmation email
        await SendOrderConfirmationEmailAsync(@event, cancellationToken);
        
        // Update inventory
        await UpdateInventoryAsync(@event, cancellationToken);
        
        // Send notification to customer
        await SendCustomerNotificationAsync(@event, cancellationToken);
    }

    private async Task SendOrderConfirmationEmailAsync(OrderCreatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending order confirmation email for order {OrderId}", @event.OrderId);
        // Implementation here
        await Task.CompletedTask;
    }

    private async Task UpdateInventoryAsync(OrderCreatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating inventory for order {OrderId}", @event.OrderId);
        // Implementation here
        await Task.CompletedTask;
    }

    private async Task SendCustomerNotificationAsync(OrderCreatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending customer notification for order {OrderId}", @event.OrderId);
        // Implementation here
        await Task.CompletedTask;
    }
}
```

### 3. Service Integration

```csharp
public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOutboxStore _outboxStore;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IOutboxStore outboxStore,
        IMessagePublisher messagePublisher,
        IUnitOfWork unitOfWork,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _outboxStore = outboxStore;
        _messagePublisher = messagePublisher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // Create order
            var order = new Order(Guid.NewGuid(), request.CustomerId);
            foreach (var item in request.Items)
            {
                order.AddItem(item.ProductId, item.Quantity, item.UnitPrice);
            }

            await _orderRepository.AddAsync(order, cancellationToken);

            // Publish order created event using outbox pattern
            var orderCreatedEvent = new OrderCreatedEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                TotalAmount = order.TotalAmount,
                Currency = order.Currency,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _messagePublisher.PublishAsync(orderCreatedEvent, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} created successfully", order.Id);
            return order.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order for customer {CustomerId}", request.CustomerId);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task CancelOrderAsync(Guid orderId, string reason, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling order {OrderId}", orderId);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
            if (order == null)
                throw new InvalidOperationException($"Order {orderId} not found");

            order.Cancel(reason);
            await _orderRepository.UpdateAsync(order, cancellationToken);

            // Publish order cancelled event using outbox pattern
            var orderCancelledEvent = new OrderCancelledEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                Reason = reason,
                CancelledAt = DateTimeOffset.UtcNow
            };

            await _messagePublisher.PublishAsync(orderCancelledEvent, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} cancelled successfully", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel order {OrderId}", orderId);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
```

### 4. Message Consumer Setup

```csharp
public class OrderEventConsumer
{
    private readonly MessageConsumer<OrderCreatedEvent> _orderCreatedConsumer;
    private readonly MessageConsumer<OrderCancelledEvent> _orderCancelledConsumer;
    private readonly MessageConsumer<PaymentProcessedEvent> _paymentProcessedConsumer;
    private readonly ILogger<OrderEventConsumer> _logger;

    public OrderEventConsumer(
        IMessageBus messageBus,
        IInboxStore inboxStore,
        ILogger<OrderEventConsumer> logger)
    {
        _logger = logger;
        
        _orderCreatedConsumer = new MessageConsumer<OrderCreatedEvent>(
            messageBus, inboxStore, HandleOrderCreated, 
            logger.CreateLogger<MessageConsumer<OrderCreatedEvent>>());
        
        _orderCancelledConsumer = new MessageConsumer<OrderCancelledEvent>(
            messageBus, inboxStore, HandleOrderCancelled,
            logger.CreateLogger<MessageConsumer<OrderCancelledEvent>>());
        
        _paymentProcessedConsumer = new MessageConsumer<PaymentProcessedEvent>(
            messageBus, inboxStore, HandlePaymentProcessed,
            logger.CreateLogger<MessageConsumer<PaymentProcessedEvent>>());
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting order event consumers");
        
        await _orderCreatedConsumer.StartAsync(cancellationToken);
        await _orderCancelledConsumer.StartAsync(cancellationToken);
        await _paymentProcessedConsumer.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping order event consumers");
        
        await _orderCreatedConsumer.StopAsync(cancellationToken);
        await _orderCancelledConsumer.StopAsync(cancellationToken);
        await _paymentProcessedConsumer.StopAsync(cancellationToken);
    }

    private async Task HandleOrderCreated(OrderCreatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling order created event for order {OrderId}", @event.OrderId);
        // Implementation here
        await Task.CompletedTask;
    }

    private async Task HandleOrderCancelled(OrderCancelledEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling order cancelled event for order {OrderId}", @event.OrderId);
        // Implementation here
        await Task.CompletedTask;
    }

    private async Task HandlePaymentProcessed(PaymentProcessedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling payment processed event for payment {PaymentId}", @event.PaymentId);
        // Implementation here
        await Task.CompletedTask;
    }
}
```

### 5. Service Registration

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add Raziee.SharedKernel
        builder.Services.AddSharedKernel();

        // Add messaging services
        builder.Services.AddScoped<IMessageBus, RabbitMQMessageBus>();
        builder.Services.AddScoped<IInboxStore, DatabaseInboxStore>();
        builder.Services.AddScoped<IOutboxStore, DatabaseOutboxStore>();
        builder.Services.AddScoped<IMessagePublisher, OutboxMessagePublisher>();
        builder.Services.AddScoped<OutboxMessageProcessor>();
        builder.Services.AddScoped<OrderEventConsumer>();

        // Add Entity Framework
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        var app = builder.Build();

        // Start message consumers
        var orderEventConsumer = app.Services.GetRequiredService<OrderEventConsumer>();
        orderEventConsumer.StartAsync();

        app.Run();
    }
}
```

## Best Practices

### 1. Message Design
- Use meaningful message types
- Include all necessary data in messages
- Design messages to be immutable
- Use versioning for message evolution

### 2. Reliability
- Implement idempotent message processing
- Use inbox pattern for duplicate prevention
- Implement proper error handling
- Use outbox pattern for reliable publishing

### 3. Performance
- Use batch processing for high-volume scenarios
- Implement proper message serialization
- Use appropriate message brokers
- Monitor message processing performance

### 4. Error Handling
- Implement retry logic for transient failures
- Use dead letter queues for failed messages
- Log errors appropriately
- Implement circuit breaker patterns

### 5. Monitoring
- Monitor message processing rates
- Track message processing failures
- Implement health checks
- Use distributed tracing

This guide provides a comprehensive foundation for implementing messaging patterns with Raziee.SharedKernel, including all the necessary patterns and practices for building reliable and scalable messaging systems.
