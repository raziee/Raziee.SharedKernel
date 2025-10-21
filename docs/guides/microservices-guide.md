# Microservices Guide

This comprehensive guide demonstrates how to use Raziee.SharedKernel to build microservices architectures with proper communication patterns, data consistency, and resilience.

## Table of Contents

- [Introduction](#introduction)
- [Microservices Architecture](#microservices-architecture)
- [Service Communication](#service-communication)
- [Message Bus Integration](#message-bus-integration)
- [Data Consistency Patterns](#data-consistency-patterns)
- [Circuit Breaker and Retry Policies](#circuit-breaker-and-retry-policies)
- [Service Discovery](#service-discovery)
- [Complete Example: E-Commerce Microservices](#complete-example-e-commerce-microservices)
- [Best Practices](#best-practices)

## Introduction

Microservices architecture breaks down applications into small, independent services that communicate over well-defined APIs. Raziee.SharedKernel provides the foundational patterns and abstractions needed to build resilient microservices.

## Microservices Architecture

### Architecture Overview

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
└───────────────────────────────────────── ────────────────┘
```

## Service Communication

### 1. Synchronous Communication

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
            return false; // Default to false for safety
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

public class UserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

### 2. Asynchronous Communication

```csharp
using Raziee.SharedKernel.Messaging;

public interface IOrderService
{
    Task<Guid> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderDto> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
}

public class OrderService : IOrderService
{
    private readonly IMessageBus _messageBus;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IOrderRepository _orderRepository;
    private readonly IUserServiceClient _userServiceClient;
    private readonly IProductServiceClient _productServiceClient;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IMessageBus messageBus,
        IMessagePublisher messagePublisher,
        IOrderRepository orderRepository,
        IUserServiceClient userServiceClient,
        IProductServiceClient productServiceClient,
        ILogger<OrderService> logger)
    {
        _messageBus = messageBus;
        _messagePublisher = messagePublisher;
        _orderRepository = orderRepository;
        _userServiceClient = userServiceClient;
        _productServiceClient = productServiceClient;
        _logger = logger;
    }

    public async Task<Guid> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

        // Validate customer exists and is active
        if (!await _userServiceClient.IsUserActiveAsync(request.CustomerId, cancellationToken))
            throw new InvalidOperationException("Customer is not active");

        // Validate products exist and are available
        foreach (var item in request.Items)
        {
            var product = await _productServiceClient.GetProductAsync(item.ProductId, cancellationToken);
            if (product == null)
                throw new InvalidOperationException($"Product {item.ProductId} not found");

            if (!product.IsAvailable || product.StockQuantity < item.Quantity)
                throw new InvalidOperationException($"Product {item.ProductId} is not available in requested quantity");
        }

        // Create order
        var order = new Order(Guid.NewGuid(), request.CustomerId);
        
        foreach (var item in request.Items)
        {
            var product = await _productServiceClient.GetProductAsync(item.ProductId, cancellationToken);
            order.AddItem(product, item.Quantity);
        }

        // Save order
        await _orderRepository.AddAsync(order, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        // Publish order created event
        var orderCreatedEvent = new OrderCreatedEvent(
            order.Id,
            order.CustomerId,
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.Items.Select(i => new OrderItemEventDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice.Amount
            }).ToList()
        );

        await _messagePublisher.PublishAsync(orderCreatedEvent, cancellationToken);

        _logger.LogInformation("Order {OrderId} created successfully", order.Id);
        return order.Id;
    }

    public async Task<OrderDto> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
            throw new InvalidOperationException($"Order {orderId} not found");

        return new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount.Amount,
            Currency = order.TotalAmount.Currency,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice.Amount
            }).ToList()
        };
    }
}

public class CreateOrderRequest
{
    public Guid CustomerId { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
```

## Message Bus Integration

### 1. Message Bus Configuration

```csharp
using Raziee.SharedKernel.Messaging;

public class MessageBusService : IMessageBus
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<MessageBusService> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public MessageBusService(IConnectionFactory connectionFactory, ILogger<MessageBusService> logger)
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

    public async Task SubscribeAsync<TMessage>(Func<TMessage, CancellationToken, Task> handler, CancellationToken cancellationToken = default)
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

### 2. Event Handlers

```csharp
// Order Service handling User events
public class UserCreatedEventHandler : IIntegrationEventHandler<UserCreatedEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<UserCreatedEventHandler> _logger;

    public UserCreatedEventHandler(IOrderRepository orderRepository, ILogger<UserCreatedEventHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task Handle(UserCreatedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling UserCreated event for user {UserId}", integrationEvent.UserId);

        // Create welcome order or initialize customer data
        // This is an example of cross-service communication
        await Task.CompletedTask;
    }
}

// Product Service handling Order events
public class OrderCreatedEventHandler : IIntegrationEventHandler<OrderCreatedEvent>
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<OrderCreatedEventHandler> _logger;

    public OrderCreatedEventHandler(IProductRepository productRepository, ILogger<OrderCreatedEventHandler> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task Handle(OrderCreatedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling OrderCreated event for order {OrderId}", integrationEvent.OrderId);

        // Update product inventory
        foreach (var item in integrationEvent.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            if (product != null)
            {
                product.UpdateStock(product.StockQuantity - item.Quantity);
                await _productRepository.UpdateAsync(product, cancellationToken);
            }
        }

        await _productRepository.SaveChangesAsync(cancellationToken);
    }
}

// User Service handling Product events
public class ProductCreatedEventHandler : IIntegrationEventHandler<ProductCreatedEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ProductCreatedEventHandler> _logger;

    public ProductCreatedEventHandler(IUserRepository userRepository, ILogger<ProductCreatedEventHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task Handle(ProductCreatedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling ProductCreated event for product {ProductId}", integrationEvent.ProductId);

        // Notify users about new products or update recommendations
        // This is an example of cross-service communication
        await Task.CompletedTask;
    }
}
```

## Data Consistency Patterns

### 1. Saga Pattern

```csharp
using Raziee.SharedKernel.DistributedTransactions;

public class OrderSaga : ISaga
{
    public Guid Id { get; }
    public SagaStatus Status { get; private set; }
    public List<SagaStep> Steps { get; }
    public Dictionary<string, object> Data { get; }

    public OrderSaga(Guid id)
    {
        Id = id;
        Status = SagaStatus.Pending;
        Steps = new List<SagaStep>();
        Data = new Dictionary<string, object>();
    }

    public void AddStep(SagaStep step)
    {
        Steps.Add(step);
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Status = SagaStatus.Running;
        
        try
        {
            foreach (var step in Steps)
            {
                await step.ExecuteAsync(cancellationToken);
                step.Status = SagaStepStatus.Completed;
            }
            
            Status = SagaStatus.Completed;
        }
        catch (Exception ex)
        {
            Status = SagaStatus.Failed;
            await CompensateAsync(cancellationToken);
            throw;
        }
    }

    public async Task CompensateAsync(CancellationToken cancellationToken = default)
    {
        Status = SagaStatus.Compensating;
        
        var completedSteps = Steps.Where(s => s.Status == SagaStepStatus.Completed).Reverse();
        
        foreach (var step in completedSteps)
        {
            try
            {
                await step.CompensateAsync(cancellationToken);
                step.Status = SagaStepStatus.Compensated;
            }
            catch (Exception ex)
            {
                // Log compensation failure but continue
                Console.WriteLine($"Failed to compensate step {step.Name}: {ex.Message}");
            }
        }
        
        Status = SagaStatus.Compensated;
    }
}

public class CreateOrderSaga : OrderSaga
{
    public CreateOrderSaga(Guid orderId, Guid customerId, List<OrderItemDto> items) : base(orderId)
    {
        Data["OrderId"] = orderId;
        Data["CustomerId"] = customerId;
        Data["Items"] = items;

        // Define saga steps
        AddStep(new ValidateCustomerStep(customerId));
        AddStep(new ReserveInventoryStep(items));
        AddStep(new CreateOrderStep(orderId, customerId, items));
        AddStep(new ProcessPaymentStep(orderId));
        AddStep(new SendConfirmationStep(orderId));
    }
}

public class ValidateCustomerStep : SagaStep
{
    private readonly Guid _customerId;
    private readonly IUserServiceClient _userServiceClient;

    public ValidateCustomerStep(Guid customerId, IUserServiceClient userServiceClient)
    {
        _customerId = customerId;
        _userServiceClient = userServiceClient;
    }

    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var isActive = await _userServiceClient.IsUserActiveAsync(_customerId, cancellationToken);
        if (!isActive)
            throw new InvalidOperationException("Customer is not active");
    }

    public override async Task CompensateAsync(CancellationToken cancellationToken = default)
    {
        // No compensation needed for validation
        await Task.CompletedTask;
    }
}

public class ReserveInventoryStep : SagaStep
{
    private readonly List<OrderItemDto> _items;
    private readonly IProductServiceClient _productServiceClient;

    public ReserveInventoryStep(List<OrderItemDto> items, IProductServiceClient productServiceClient)
    {
        _items = items;
        _productServiceClient = productServiceClient;
    }

    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        foreach (var item in _items)
        {
            await _productServiceClient.ReserveInventoryAsync(item.ProductId, item.Quantity, cancellationToken);
        }
    }

    public override async Task CompensateAsync(CancellationToken cancellationToken = default)
    {
        foreach (var item in _items)
        {
            await _productServiceClient.ReleaseInventoryAsync(item.ProductId, item.Quantity, cancellationToken);
        }
    }
}
```

### 2. Outbox Pattern

```csharp
using Raziee.SharedKernel.Messaging;

public class OutboxEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}

public class OutboxService : IOutboxService
{
    private readonly IOutboxStore _outboxStore;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<OutboxService> _logger;

    public OutboxService(IOutboxStore outboxStore, IMessagePublisher messagePublisher, ILogger<OutboxService> logger)
    {
        _outboxStore = outboxStore;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    public async Task AddEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var outboxEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = typeof(TEvent).Name,
            EventData = JsonSerializer.Serialize(@event),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _outboxStore.AddEventAsync(outboxEvent, cancellationToken);
    }

    public async Task ProcessEventsAsync(CancellationToken cancellationToken = default)
    {
        var events = await _outboxStore.GetUnprocessedEventsAsync(cancellationToken);
        
        foreach (var @event in events)
        {
            try
            {
                await ProcessEventAsync(@event, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox event {EventId}", @event.Id);
                await _outboxStore.MarkEventAsFailedAsync(@event.Id, ex.Message, cancellationToken);
            }
        }
    }

    private async Task ProcessEventAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        var eventType = Type.GetType(outboxEvent.EventType);
        if (eventType == null)
        {
            _logger.LogWarning("Unknown event type {EventType}", outboxEvent.EventType);
            return;
        }

        var eventData = JsonSerializer.Deserialize(outboxEvent.EventData, eventType);
        if (eventData == null)
        {
            _logger.LogWarning("Failed to deserialize event data for {EventId}", outboxEvent.Id);
            return;
        }

        await _messagePublisher.PublishAsync(eventData, cancellationToken);
        await _outboxStore.MarkEventAsProcessedAsync(outboxEvent.Id, cancellationToken);
    }
}
```

## Circuit Breaker and Retry Policies

### 1. Circuit Breaker Implementation

```csharp
using Raziee.SharedKernel.ServiceCommunication;

public class CircuitBreakerService : ICircuitBreaker
{
    private readonly ILogger<CircuitBreakerService> _logger;
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount = 0;
    private DateTimeOffset _lastFailureTime = DateTimeOffset.MinValue;
    private readonly int _failureThreshold;
    private readonly TimeSpan _timeout;
    private readonly TimeSpan _retryTimeout;

    public CircuitBreakerService(
        ILogger<CircuitBreakerService> logger,
        int failureThreshold = 5,
        TimeSpan timeout = default,
        TimeSpan retryTimeout = default)
    {
        _logger = logger;
        _failureThreshold = failureThreshold;
        _timeout = timeout == default ? TimeSpan.FromMinutes(1) : timeout;
        _retryTimeout = retryTimeout == default ? TimeSpan.FromMinutes(5) : retryTimeout;
    }

    public string Name => "DefaultCircuitBreaker";
    public CircuitBreakerState State => _state;

    public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        if (_state == CircuitBreakerState.Open)
        {
            if (DateTimeOffset.UtcNow - _lastFailureTime < _retryTimeout)
            {
                throw new CircuitBreakerOpenException("Circuit breaker is open");
            }
            
            _state = CircuitBreakerState.HalfOpen;
            _logger.LogInformation("Circuit breaker moved to half-open state");
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

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation();
            return true;
        }, cancellationToken);
    }

    private void OnSuccess()
    {
        _failureCount = 0;
        _state = CircuitBreakerState.Closed;
    }

    private void OnFailure()
    {
        _failureCount++;
        _lastFailureTime = DateTimeOffset.UtcNow;

        if (_failureCount >= _failureThreshold)
        {
            _state = CircuitBreakerState.Open;
            _logger.LogWarning("Circuit breaker opened after {FailureCount} failures", _failureCount);
        }
    }
}

public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message)
    {
    }
}
```

### 2. Retry Policy Implementation

```csharp
using Raziee.SharedKernel.ServiceCommunication;

public class RetryPolicyService : IRetryPolicy
{
    private readonly ILogger<RetryPolicyService> _logger;
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;
    private readonly double _backoffMultiplier;
    private readonly TimeSpan _maxDelay;

    public RetryPolicyService(
        ILogger<RetryPolicyService> logger,
        int maxRetries = 3,
        TimeSpan baseDelay = default,
        double backoffMultiplier = 2.0,
        TimeSpan maxDelay = default)
    {
        _logger = logger;
        _maxRetries = maxRetries;
        _baseDelay = baseDelay == default ? TimeSpan.FromSeconds(1) : baseDelay;
        _backoffMultiplier = backoffMultiplier;
        _maxDelay = maxDelay == default ? TimeSpan.FromMinutes(5) : maxDelay;
    }

    public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt == _maxRetries)
                {
                    _logger.LogError(ex, "Operation failed after {MaxRetries} retries", _maxRetries);
                    throw;
                }

                if (!ShouldRetry(ex))
                {
                    _logger.LogWarning(ex, "Operation failed with non-retryable exception");
                    throw;
                }

                var delay = CalculateDelay(attempt);
                _logger.LogWarning(ex, "Operation failed (attempt {Attempt}/{MaxRetries}), retrying in {Delay}ms", 
                    attempt + 1, _maxRetries + 1, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("Operation failed");
    }

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation();
            return true;
        }, cancellationToken);
    }

    private bool ShouldRetry(Exception exception)
    {
        return exception is HttpRequestException ||
               exception is TaskCanceledException ||
               exception is TimeoutException ||
               (exception is InvalidOperationException && exception.Message.Contains("timeout"));
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(_backoffMultiplier, attempt));
        return delay > _maxDelay ? _maxDelay : delay;
    }
}
```

## Service Discovery

### 1. Service Discovery Implementation

```csharp
using Raziee.SharedKernel.ServiceCommunication;

public class ConsulServiceDiscovery : IServiceDiscovery
{
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ConsulServiceDiscovery> _logger;

    public ConsulServiceDiscovery(IConsulClient consulClient, ILogger<ConsulServiceDiscovery> logger)
    {
        _consulClient = consulClient;
        _logger = logger;
    }

    public async Task<IEnumerable<ServiceEndpoint>> DiscoverServicesAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Discovering services for {ServiceName}", serviceName);

        var services = await _consulClient.Health.Service(serviceName, string.Empty, true, cancellationToken);
        
        var endpoints = services.Response.Select(service => new ServiceEndpoint
        {
            Id = service.Service.ID,
            Name = service.Service.Service,
            Url = $"http://{service.Service.Address}:{service.Service.Port}",
            Health = service.Checks.All(c => c.Status == HealthStatus.Passing) ? ServiceHealth.Healthy : ServiceHealth.Unhealthy
        });

        _logger.LogDebug("Found {Count} services for {ServiceName}", endpoints.Count(), serviceName);
        return endpoints;
    }

    public async Task<ServiceEndpoint?> DiscoverServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var services = await DiscoverServicesAsync(serviceName, cancellationToken);
        return services.FirstOrDefault(s => s.Health == ServiceHealth.Healthy);
    }

    public async Task RegisterServiceAsync(string serviceName, ServiceEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Registering service {ServiceName} at {Endpoint}", serviceName, endpoint.Url);

        var registration = new AgentServiceRegistration
        {
            ID = endpoint.Id,
            Name = serviceName,
            Address = endpoint.Url,
            Port = GetPortFromUrl(endpoint.Url),
            Check = new AgentServiceCheck
            {
                HTTP = $"{endpoint.Url}/health",
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5)
            }
        };

        await _consulClient.Agent.ServiceRegister(registration, cancellationToken);
    }

    public async Task UnregisterServiceAsync(string serviceName, ServiceEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Unregistering service {ServiceName} at {Endpoint}", serviceName, endpoint.Url);
        await _consulClient.Agent.ServiceDeregister(endpoint.Id, cancellationToken);
    }

    private int GetPortFromUrl(string url)
    {
        var uri = new Uri(url);
        return uri.Port;
    }
}

public class ServiceEndpoint
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public ServiceHealth Health { get; set; }
}

public enum ServiceHealth
{
    Healthy,
    Unhealthy,
    Unknown
}
```

## Complete Example: E-Commerce Microservices

### 1. User Service

```csharp
// UserService/Controllers/UsersController.cs
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateUser(CreateUserRequest request)
    {
        var command = new CreateUserCommand
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email
        };

        var userId = await _mediator.Send(command);
        return Ok(userId);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        var query = new GetUserQuery { UserId = id };
        var user = await _mediator.Send(query);
        return Ok(user);
    }

    [HttpGet("{id}/profile")]
    public async Task<ActionResult<UserProfileDto>> GetUserProfile(Guid id)
    {
        var query = new GetUserProfileQuery { UserId = id };
        var profile = await _mediator.Send(query);
        return Ok(profile);
    }

    [HttpGet("{id}/active")]
    public async Task<ActionResult<bool>> IsUserActive(Guid id)
    {
        var query = new IsUserActiveQuery { UserId = id };
        var isActive = await _mediator.Send(query);
        return Ok(isActive);
    }
}

// UserService/Application/Commands/CreateUserCommand.cs
public class CreateUserCommand : ICommand<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating user with email {Email}", request.Email);

        var user = new User(Guid.NewGuid(), request.FirstName, request.LastName, new Email(request.Email));
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} created successfully", user.Id);
        return user.Id;
    }
}
```

### 2. Product Service

```csharp
// ProductService/Controllers/ProductsController.cs
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IMediator mediator, ILogger<ProductsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateProduct(CreateProductRequest request)
    {
        var command = new CreateProductCommand
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Currency = request.Currency,
            StockQuantity = request.StockQuantity
        };

        var productId = await _mediator.Send(command);
        return Ok(productId);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
    {
        var query = new GetProductQuery { ProductId = id };
        var product = await _mediator.Send(query);
        return Ok(product);
    }

    [HttpPost("{id}/reserve")]
    public async Task<ActionResult> ReserveInventory(Guid id, ReserveInventoryRequest request)
    {
        var command = new ReserveInventoryCommand
        {
            ProductId = id,
            Quantity = request.Quantity
        };

        await _mediator.Send(command);
        return Ok();
    }

    [HttpPost("{id}/release")]
    public async Task<ActionResult> ReleaseInventory(Guid id, ReleaseInventoryRequest request)
    {
        var command = new ReleaseInventoryCommand
        {
            ProductId = id,
            Quantity = request.Quantity
        };

        await _mediator.Send(command);
        return Ok();
    }
}
```

### 3. Order Service

```csharp
// OrderService/Controllers/OrdersController.cs
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateOrder(CreateOrderRequest request)
    {
        var command = new CreateOrderCommand
        {
            CustomerId = request.CustomerId,
            Items = request.Items.Select(item => new OrderItemDto
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList()
        };

        var orderId = await _mediator.Send(command);
        return Ok(orderId);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid id)
    {
        var query = new GetOrderQuery { OrderId = id };
        var order = await _mediator.Send(query);
        return Ok(order);
    }

    [HttpPost("{id}/confirm")]
    public async Task<ActionResult> ConfirmOrder(Guid id)
    {
        var command = new ConfirmOrderCommand { OrderId = id };
        await _mediator.Send(command);
        return Ok();
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult> CancelOrder(Guid id, CancelOrderRequest request)
    {
        var command = new CancelOrderCommand
        {
            OrderId = id,
            Reason = request.Reason
        };

        await _mediator.Send(command);
        return Ok();
    }
}
```

## Best Practices

### 1. Service Design
- Keep services focused on a single business capability
- Use domain-driven design principles
- Implement proper error handling and logging
- Design for failure and resilience

### 2. Communication Patterns
- Use asynchronous communication for loose coupling
- Implement circuit breakers and retry policies
- Use message queues for reliable communication
- Implement proper service discovery

### 3. Data Management
- Use database per service pattern
- Implement eventual consistency
- Use saga pattern for distributed transactions
- Implement proper data migration strategies

### 4. Security
- Implement proper authentication and authorization
- Use API gateways for security policies
- Implement proper secret management
- Use HTTPS for all communications

### 5. Monitoring and Observability
- Implement distributed tracing
- Use structured logging
- Implement health checks
- Monitor service performance and availability

This guide provides a comprehensive foundation for building microservices with Raziee.SharedKernel, including all the necessary patterns and practices for building resilient, scalable, and maintainable microservices architectures.
