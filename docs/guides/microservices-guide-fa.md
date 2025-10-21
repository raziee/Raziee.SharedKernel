# راهنمای Microservices

این راهنمای جامع نحوه استفاده از Raziee.SharedKernel برای ساخت معماری‌های microservices با الگوهای ارتباطی مناسب، سازگاری داده و مقاومت را نشان می‌دهد.

## فهرست مطالب

- [مقدمه](#مقدمه)
- [معماری Microservices](#معماری-microservices)
- [ارتباط سرویس](#ارتباط-سرویس)
- [ادغام Message Bus](#ادغام-message-bus)
- [الگوهای سازگاری داده](#الگوهای-سازگاری-داده)
- [Circuit Breaker و Retry Policies](#circuit-breaker-و-retry-policies)
- [Service Discovery](#service-discovery)
- [مثال کامل: Microservices تجارت الکترونیک](#مثال-کامل-microservices-تجارت-الکترونیک)
- [بهترین شیوه‌ها](#بهترین-شیوه‌ها)

## مقدمه

معماری microservices برنامه‌ها را به سرویس‌های کوچک و مستقل تقسیم می‌کند که از طریق APIهای تعریف‌شده با هم ارتباط برقرار می‌کنند. Raziee.SharedKernel الگوها و انتزاعات پایه‌ای لازم برای ساخت microservices مقاوم را ارائه می‌دهد.

## معماری Microservices

### نمای کلی معماری

```
┌──────────────────────────────────────────────────────────┐
│                    Microservices Ecosystem               │
├──────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐       │
│  │   User      │  │  Product    │  │   Order     │       │
│  │  Service    │  │  Service    │  │  Service    │       │
│  │             │  │             │  │             │       │
│  │ • Auth      │  │ • Catalog   │  │ • Orders    │       │
│  │ • Profile   │  │ • Inventory │  │ • Payments  │       │
│  │ • Roles     │  │ • Pricing   │  │ • Shipping  │       │
│  └─────────────┘  └─────────────┘  └─────────────┘       │
│         │               │               │                │
│         └───────────────┼───────────────┘                │
│                         │                                │
│  ┌────────────────────────────────────────────────────┐  │
│  │              Message Bus (RabbitMQ/Kafka)          │  │
│  │         (Event-Driven Communication)               │  │
│  │                                                    │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │  │
│  │  │   Topics    │  │   Queues    │  │   Routing   │ │  │
│  │  │             │  │             │  │             │ │  │
│  │  │ • user.*    │  │ • user.q    │  │ • Direct    │ │  │
│  │  │ • order.*   │  │ • order.q   │  │ • Fanout    │ │  │
│  │  │ • product.* │  │ • product.q │  │ • Topic     │ │  │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │  │
│  └────────────────────────────────────────────────────┘  │
│                         │                                │
│  ┌────────────────────────────────────────────────────┐  │
│  │              Service Discovery (Consul/Eureka)     │  │
│  │                                                    │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │  │
│  │  │   Health    │  │   Load      │  │   Circuit   │ │  │
│  │  │   Checks    │  │   Balance   │  │   Breaker   │ │  │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │  │
│  └────────────────────────────────────────────────────┘  │
│                         │                                │
│  ┌────────────────────────────────────────────────────┐  │
│  │              API Gateway (Kong/Envoy)              │  │
│  │                                                    │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │  │
│  │  │   Routing   │  │   Auth      │  │   Rate      │ │  │
│  │  │             │  │             │  │   Limit     │ │  │
│  │  │ • Path      │  │ • JWT       │  │ • Throttle  │ │  │
│  │  │ • Host      │  │ • OAuth     │  │ • Quota     │ │  │
│  │  │ • Method    │  │ • API Key   │  │ • Burst     │ │  │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
```

## ارتباط سرویس

### 1. ارتباط همزمان

```csharp
using Raziee.SharedKernel.ServiceCommunication;

public interface IUserServiceClient
{
    Task<UserDto> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsUserActiveAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserProfileDto> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class UserServiceClient : IUserServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly IRetryPolicy _retryPolicy;
    private readonly ILogger<UserServiceClient> _logger;

    public UserServiceClient(
        HttpClient httpClient,
        IServiceDiscovery serviceDiscovery,
        ICircuitBreaker circuitBreaker,
        IRetryPolicy retryPolicy,
        ILogger<UserServiceClient> logger)
    {
        _httpClient = httpClient;
        _serviceDiscovery = serviceDiscovery;
        _circuitBreaker = circuitBreaker;
        _retryPolicy = retryPolicy;
        _logger = logger;
    }

    public async Task<UserDto> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var serviceEndpoint = await _serviceDiscovery.DiscoverServiceAsync("UserService", cancellationToken);
                if (serviceEndpoint == null)
                    throw new ServiceUnavailableException("UserService is not available");

                var response = await _httpClient.GetAsync($"{serviceEndpoint.Url}/api/users/{userId}", cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<UserDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            });
        });
    }

    public async Task<bool> IsUserActiveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await GetUserAsync(userId, cancellationToken);
            return user?.IsActive ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check user status for {UserId}", userId);
            return false; // پیش‌فرض false برای امنیت
        }
    }

    public async Task<UserProfileDto> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var serviceEndpoint = await _serviceDiscovery.DiscoverServiceAsync("UserService", cancellationToken);
                if (serviceEndpoint == null)
                    throw new ServiceUnavailableException("UserService is not available");

                var response = await _httpClient.GetAsync($"{serviceEndpoint.Url}/api/users/{userId}/profile", cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<UserProfileDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            });
        });
    }
}
```

### 2. ارتباط غیرهمزمان

```csharp
public interface IMessageBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
    Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler, CancellationToken cancellationToken = default);
    Task UnsubscribeAsync<TEvent>(Func<TEvent, Task> handler, CancellationToken cancellationToken = default);
}

public class RabbitMQMessageBus : IMessageBus
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQMessageBus> _logger;

    public RabbitMQMessageBus(IConnection connection, ILogger<RabbitMQMessageBus> logger)
    {
        _connection = connection;
        _channel = _connection.CreateModel();
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Publishing event {EventType}", typeof(TEvent).Name);

        var eventName = typeof(TEvent).Name;
        var message = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(message);

        _channel.BasicPublish(
            exchange: "events",
            routingKey: eventName,
            basicProperties: null,
            body: body);

        _logger.LogDebug("Event {EventType} published successfully", eventName);
        await Task.CompletedTask;
    }

    public async Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler, CancellationToken cancellationToken = default)
    {
        var eventName = typeof(TEvent).Name;
        var queueName = $"{eventName}.queue";

        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queueName, "events", eventName);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var @event = JsonSerializer.Deserialize<TEvent>(message);

                await handler(@event!);
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event {EventType}", eventName);
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(queueName, autoAck: false, consumer);
        _logger.LogDebug("Subscribed to event {EventType}", eventName);
        await Task.CompletedTask;
    }

    public async Task UnsubscribeAsync<TEvent>(Func<TEvent, Task> handler, CancellationToken cancellationToken = default)
    {
        // منطق unsubscribe
        await Task.CompletedTask;
    }
}
```

## ادغام Message Bus

### 1. Message Publisher

```csharp
public interface IMessagePublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
    Task PublishAsync<TEvent>(TEvent @event, string topic, CancellationToken cancellationToken = default);
}

public class MessagePublisher : IMessagePublisher
{
    private readonly IMessageBus _messageBus;
    private readonly IOutboxStore _outboxStore;
    private readonly ILogger<MessagePublisher> _logger;

    public MessagePublisher(
        IMessageBus messageBus,
        IOutboxStore outboxStore,
        ILogger<MessagePublisher> logger)
    {
        _messageBus = messageBus;
        _outboxStore = outboxStore;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Publishing event {EventType}", typeof(TEvent).Name);

        try
        {
            await _messageBus.PublishAsync(@event, cancellationToken);
            _logger.LogDebug("Event {EventType} published successfully", typeof(TEvent).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", typeof(TEvent).Name);
            
            // ذخیره در outbox برای retry بعدی
            await _outboxStore.StoreAsync(@event, cancellationToken);
            throw;
        }
    }

    public async Task PublishAsync<TEvent>(TEvent @event, string topic, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Publishing event {EventType} to topic {Topic}", typeof(TEvent).Name, topic);

        try
        {
            await _messageBus.PublishAsync(@event, topic, cancellationToken);
            _logger.LogDebug("Event {EventType} published to topic {Topic} successfully", typeof(TEvent).Name, topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} to topic {Topic}", typeof(TEvent).Name, topic);
            
            // ذخیره در outbox برای retry بعدی
            await _outboxStore.StoreAsync(@event, topic, cancellationToken);
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

        // Subscribe to various events
        await SubscribeToEventsAsync(cancellationToken);

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

    private async Task SubscribeToEventsAsync(CancellationToken cancellationToken)
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

    private async Task HandleUserCreatedEvent(UserCreatedEvent @event)
    {
        _logger.LogInformation("Handling UserCreatedEvent for user {UserId}", @event.UserId);

        try
        {
            // بررسی duplicate message
            if (await _inboxStore.HasProcessedAsync(@event.Id))
            {
                _logger.LogDebug("Event {EventId} already processed", @event.Id);
                return;
            }

            // منطق پردازش رویداد
            var handler = _serviceProvider.GetService<IIntegrationEventHandler<UserCreatedEvent>>();
            if (handler != null)
            {
                await handler.Handle(@event);
            }

            // ذخیره در inbox
            await _inboxStore.StoreAsync(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UserCreatedEvent for user {UserId}", @event.UserId);
            throw;
        }
    }

    private async Task HandleUserUpdatedEvent(UserUpdatedEvent @event)
    {
        _logger.LogInformation("Handling UserUpdatedEvent for user {UserId}", @event.UserId);
        // منطق مشابه
        await Task.CompletedTask;
    }

    private async Task HandleProductCreatedEvent(ProductCreatedEvent @event)
    {
        _logger.LogInformation("Handling ProductCreatedEvent for product {ProductId}", @event.ProductId);
        // منطق مشابه
        await Task.CompletedTask;
    }

    private async Task HandleProductUpdatedEvent(ProductUpdatedEvent @event)
    {
        _logger.LogInformation("Handling ProductUpdatedEvent for product {ProductId}", @event.ProductId);
        // منطق مشابه
        await Task.CompletedTask;
    }

    private async Task HandleOrderCreatedEvent(OrderCreatedEvent @event)
    {
        _logger.LogInformation("Handling OrderCreatedEvent for order {OrderId}", @event.OrderId);
        // منطق مشابه
        await Task.CompletedTask;
    }

    private async Task HandleOrderStatusChangedEvent(OrderStatusChangedEvent @event)
    {
        _logger.LogInformation("Handling OrderStatusChangedEvent for order {OrderId}", @event.OrderId);
        // منطق مشابه
        await Task.CompletedTask;
    }
}
```

## الگوهای سازگاری داده

### 1. Saga Pattern

```csharp
public interface ISagaOrchestrator
{
    Task StartSagaAsync<TData>(TData data, CancellationToken cancellationToken = default);
    Task CompleteSagaAsync(Guid sagaId, CancellationToken cancellationToken = default);
    Task CompensateSagaAsync(Guid sagaId, CancellationToken cancellationToken = default);
}

public class OrderSagaOrchestrator : ISagaOrchestrator
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPaymentService _paymentService;
    private readonly IShippingService _shippingService;
    private readonly ILogger<OrderSagaOrchestrator> _logger;

    public OrderSagaOrchestrator(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IPaymentService paymentService,
        IShippingService shippingService,
        ILogger<OrderSagaOrchestrator> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _paymentService = paymentService;
        _shippingService = shippingService;
        _logger = logger;
    }

    public async Task StartSagaAsync<TData>(TData data, CancellationToken cancellationToken = default)
    {
        if (data is not CreateOrderRequest request)
            throw new ArgumentException("Invalid saga data type");

        _logger.LogInformation("Starting order saga for customer {CustomerId}", request.CustomerId);

        var sagaId = Guid.NewGuid();
        var saga = new OrderSaga(sagaId, request.CustomerId);

        try
        {
            // Step 1: Create order
            var order = new Order(Guid.NewGuid(), request.CustomerId);
            foreach (var item in request.Items)
            {
                order.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);
            }
            await _orderRepository.AddAsync(order, cancellationToken);
            saga.AddStep(new OrderCreatedStep(order.Id));

            // Step 2: Reserve inventory
            foreach (var item in request.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
                if (product == null)
                    throw new InvalidOperationException($"Product {item.ProductId} not found");

                if (product.StockQuantity < item.Quantity)
                    throw new InvalidOperationException($"Insufficient stock for product {item.ProductId}");

                product.ReserveStock(item.Quantity);
                await _productRepository.UpdateAsync(product, cancellationToken);
            }
            saga.AddStep(new InventoryReservedStep(request.Items));

            // Step 3: Process payment
            var paymentResult = await _paymentService.ProcessPaymentAsync(new ProcessPaymentRequest
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                CustomerId = request.CustomerId
            }, cancellationToken);

            if (!paymentResult.Success)
                throw new InvalidOperationException("Payment processing failed");

            saga.AddStep(new PaymentProcessedStep(paymentResult.TransactionId));

            // Step 4: Create shipping
            var shippingResult = await _shippingService.CreateShipmentAsync(new CreateShipmentRequest
            {
                OrderId = order.Id,
                CustomerId = request.CustomerId,
                Items = request.Items
            }, cancellationToken);

            if (!shippingResult.Success)
                throw new InvalidOperationException("Shipping creation failed");

            saga.AddStep(new ShippingCreatedStep(shippingResult.ShipmentId));

            // Complete saga
            await CompleteSagaAsync(sagaId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order saga failed for customer {CustomerId}", request.CustomerId);
            await CompensateSagaAsync(sagaId, cancellationToken);
            throw;
        }
    }

    public async Task CompleteSagaAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Completing saga {SagaId}", sagaId);
        // منطق تکمیل saga
        await Task.CompletedTask;
    }

    public async Task CompensateSagaAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Compensating saga {SagaId}", sagaId);
        // منطق جبران saga
        await Task.CompletedTask;
    }
}
```

### 2. Outbox Pattern

```csharp
public interface IOutboxStore
{
    Task StoreAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
    Task StoreAsync<TEvent>(TEvent @event, string topic, CancellationToken cancellationToken = default);
    Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
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
}

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
            Id = Guid.NewGuid(),
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
            Id = Guid.NewGuid(),
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
}
```

## Circuit Breaker و Retry Policies

### 1. Circuit Breaker

```csharp
public interface ICircuitBreaker
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
    Task ExecuteAsync(Func<Task> operation);
}

public class CircuitBreaker : ICircuitBreaker
{
    private readonly CircuitBreakerOptions _options;
    private readonly ILogger<CircuitBreaker> _logger;
    private CircuitState _state = CircuitState.Closed;
    private int _failureCount = 0;
    private DateTimeOffset _lastFailureTime = DateTimeOffset.MinValue;

    public CircuitBreaker(CircuitBreakerOptions options, ILogger<CircuitBreaker> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        if (_state == CircuitState.Open)
        {
            if (DateTimeOffset.UtcNow - _lastFailureTime < _options.Timeout)
            {
                throw new CircuitBreakerOpenException("Circuit breaker is open");
            }
            else
            {
                _state = CircuitState.HalfOpen;
                _logger.LogInformation("Circuit breaker moved to half-open state");
            }
        }

        try
        {
            var result = await operation();
            OnSuccess();
            return result;
        }
        catch (Exception ex)
        {
            OnFailure();
            throw;
        }
    }

    public async Task ExecuteAsync(Func<Task> operation)
    {
        await ExecuteAsync(async () =>
        {
            await operation();
            return true;
        });
    }

    private void OnSuccess()
    {
        _failureCount = 0;
        _state = CircuitState.Closed;
        _logger.LogDebug("Circuit breaker operation succeeded");
    }

    private void OnFailure()
    {
        _failureCount++;
        _lastFailureTime = DateTimeOffset.UtcNow;

        if (_failureCount >= _options.FailureThreshold)
        {
            _state = CircuitState.Open;
            _logger.LogWarning("Circuit breaker opened after {FailureCount} failures", _failureCount);
        }
    }
}

public class CircuitBreakerOptions
{
    public int FailureThreshold { get; set; } = 5;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
}

public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}
```

### 2. Retry Policy

```csharp
public interface IRetryPolicy
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
    Task ExecuteAsync(Func<Task> operation);
}

public class RetryPolicy : IRetryPolicy
{
    private readonly RetryPolicyOptions _options;
    private readonly ILogger<RetryPolicy> _logger;

    public RetryPolicy(RetryPolicyOptions options, ILogger<RetryPolicy> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < _options.MaxRetryCount)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (ShouldRetry(ex))
            {
                lastException = ex;
                attempt++;

                if (attempt < _options.MaxRetryCount)
                {
                    var delay = CalculateDelay(attempt);
                    _logger.LogWarning(ex, "Operation failed, retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})", 
                        delay.TotalMilliseconds, attempt, _options.MaxRetryCount);
                    
                    await Task.Delay(delay);
                }
            }
        }

        _logger.LogError(lastException, "Operation failed after {MaxRetries} attempts", _options.MaxRetryCount);
        throw lastException!;
    }

    public async Task ExecuteAsync(Func<Task> operation)
    {
        await ExecuteAsync(async () =>
        {
            await operation();
            return true;
        });
    }

    private bool ShouldRetry(Exception ex)
    {
        return _options.RetryableExceptions.Any(type => type.IsInstanceOfType(ex));
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        return _options.BackoffStrategy switch
        {
            BackoffStrategy.Fixed => _options.Delay,
            BackoffStrategy.Exponential => TimeSpan.FromMilliseconds(_options.Delay.TotalMilliseconds * Math.Pow(2, attempt - 1)),
            BackoffStrategy.Linear => TimeSpan.FromMilliseconds(_options.Delay.TotalMilliseconds * attempt),
            _ => _options.Delay
        };
    }
}

public class RetryPolicyOptions
{
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);
    public BackoffStrategy BackoffStrategy { get; set; } = BackoffStrategy.Exponential;
    public List<Type> RetryableExceptions { get; set; } = new()
    {
        typeof(HttpRequestException),
        typeof(TimeoutException),
        typeof(TaskCanceledException)
    };
}

public enum BackoffStrategy
{
    Fixed,
    Exponential,
    Linear
}
```

## Service Discovery

### 1. Service Discovery Interface

```csharp
public interface IServiceDiscovery
{
    Task<ServiceEndpoint?> DiscoverServiceAsync(string serviceName, CancellationToken cancellationToken = default);
    Task RegisterServiceAsync(ServiceRegistration registration, CancellationToken cancellationToken = default);
    Task UnregisterServiceAsync(string serviceName, string instanceId, CancellationToken cancellationToken = default);
}

public class ServiceEndpoint
{
    public string Url { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTimeOffset LastHeartbeat { get; set; }
}

public class ServiceRegistration
{
    public string ServiceName { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);
}
```

## مثال کامل: Microservices تجارت الکترونیک

### 1. User Service

```csharp
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IMessagePublisher messagePublisher,
        ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateUser(CreateUserRequest request)
    {
        _logger.LogInformation("Creating user with email {Email}", request.Email);

        var user = new User(Guid.NewGuid(), request.Email, request.FirstName, request.LastName);
        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // انتشار رویداد
        var userCreatedEvent = new UserCreatedEvent
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        };

        await _messagePublisher.PublishAsync(userCreatedEvent);

        _logger.LogInformation("User {UserId} created successfully", user.Id);
        return Ok(user.Id);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound();

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }
}
```

### 2. Order Service

```csharp
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserServiceClient _userServiceClient;
    private readonly IProductServiceClient _productServiceClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IOrderRepository orderRepository,
        IUserServiceClient userServiceClient,
        IProductServiceClient productServiceClient,
        IUnitOfWork unitOfWork,
        IMessagePublisher messagePublisher,
        ILogger<OrdersController> logger)
    {
        _orderRepository = orderRepository;
        _userServiceClient = userServiceClient;
        _productServiceClient = productServiceClient;
        _unitOfWork = unitOfWork;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateOrder(CreateOrderRequest request)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

        // اعتبارسنجی مشتری
        var isUserActive = await _userServiceClient.IsUserActiveAsync(request.CustomerId);
        if (!isUserActive)
            return BadRequest("Customer is not active");

        // اعتبارسنجی محصولات
        foreach (var item in request.Items)
        {
            var product = await _productServiceClient.GetProductAsync(item.ProductId);
            if (product == null)
                return BadRequest($"Product {item.ProductId} not found");

            if (product.StockQuantity < item.Quantity)
                return BadRequest($"Insufficient stock for product {item.ProductId}");
        }

        // ایجاد سفارش
        var order = new Order(Guid.NewGuid(), request.CustomerId);
        foreach (var item in request.Items)
        {
            order.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);
        }

        await _orderRepository.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        // انتشار رویداد
        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        await _messagePublisher.PublishAsync(orderCreatedEvent);

        _logger.LogInformation("Order {OrderId} created successfully", order.Id);
        return Ok(order.Id);
    }
}
```

## بهترین شیوه‌ها

### 1. طراحی سرویس
- سرویس‌ها را بر اساس قابلیت‌های کسب‌وکار طراحی کنید
- از database per service استفاده کنید
- API contracts را به دقت تعریف کنید
- versioning را پیاده‌سازی کنید

### 2. ارتباط سرویس
- از async communication استفاده کنید
- از circuit breakers استفاده کنید
- retry policies را پیاده‌سازی کنید
- timeout ها را تنظیم کنید

### 3. مدیریت داده
- از eventual consistency استفاده کنید
- saga pattern را پیاده‌سازی کنید
- outbox pattern را استفاده کنید
- data migration strategies را برنامه‌ریزی کنید

### 4. امنیت
- authentication و authorization را پیاده‌سازی کنید
- از API gateway استفاده کنید
- rate limiting را تنظیم کنید
- audit logging را پیاده‌سازی کنید

### 5. Monitoring
- health checks را پیاده‌سازی کنید
- metrics و logging را تنظیم کنید
- distributed tracing را استفاده کنید
- alerting مناسب ایجاد کنید

این راهنما پایه جامعی برای پیاده‌سازی microservices با Raziee.SharedKernel ارائه می‌دهد، شامل تمام الگوها و شیوه‌های لازم برای ساخت سیستم‌های توزیع‌شده قابل اعتماد و مقیاس‌پذیر.
