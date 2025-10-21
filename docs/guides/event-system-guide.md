# Event System Guide

This comprehensive guide explains the event system in Raziee.SharedKernel, covering Domain Events, Integration Events, Inbox Pattern, and Outbox Pattern.

## Table of Contents

- [Introduction](#introduction)
- [Event System Architecture](#event-system-architecture)
- [Domain Events](#domain-events)
- [Integration Events](#integration-events)
- [Inbox Pattern](#inbox-pattern)
- [Outbox Pattern](#outbox-pattern)
- [Event Flow Examples](#event-flow-examples)
- [Best Practices](#best-practices)
- [Migration Strategies](#migration-strategies)

## Introduction

The event system in Raziee.SharedKernel provides a robust foundation for building event-driven architectures. It supports both modular monolith and microservices patterns through a comprehensive set of interfaces and implementations.

### Key Components

1. **Domain Events** - Internal events within a bounded context
2. **Integration Events** - Cross-module communication events
3. **Inbox Pattern** - Idempotent message processing
4. **Outbox Pattern** - Reliable message delivery

## Event System Architecture

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

Domain events represent something important that happened in the domain. They are used to communicate between aggregates and trigger side effects within the same bounded context.

### Interface Definition

```csharp
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the domain event.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the date and time when the domain event occurred.
    /// </summary>
    DateTimeOffset OccurredOn { get; }

    /// <summary>
    /// Gets the version of the aggregate that raised this event.
    /// This is used for optimistic concurrency control.
    /// </summary>
    int Version { get; }
}
```

### Base Implementation

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

### Example Usage

```csharp
// Domain Event Definition
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
        _logger.LogInformation("Processing UserCreated domain event for user {UserId}", domainEvent.UserId);

        // Internal business logic
        // ...

        // Convert to integration event for cross-module communication
        var integrationEvent = new UserCreatedIntegrationEvent(
            domainEvent.UserId,
            domainEvent.Email,
            domainEvent.FirstName,
            domainEvent.LastName
        );

        // Store in outbox for reliable delivery
        await _outboxStore.StoreAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(UserCreatedIntegrationEvent).Name,
            MessageContent = JsonSerializer.Serialize(integrationEvent),
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
    }
}
```

### Key Characteristics

- **Scope**: Within a single bounded context
- **Purpose**: Internal business logic and side effects
- **Versioning**: Includes aggregate version for concurrency control
- **Processing**: Synchronous within the same transaction

## Integration Events

Integration events are used for communication between modules in a modular monolith or between services in a microservices architecture.

### Interface Definition

```csharp
public interface IIntegrationEvent
{
    /// <summary>
    /// Gets the unique identifier of the integration event.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the date and time when the integration event occurred.
    /// </summary>
    DateTimeOffset OccurredOn { get; }

    /// <summary>
    /// Gets the source module that raised the event.
    /// </summary>
    string SourceModule { get; }
}
```

### Example Implementation

```csharp
public class UserCreatedIntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; }
    public DateTimeOffset OccurredOn { get; }
    public string SourceModule { get; }
    public Guid UserId { get; }
    public string Email { get; }
    public string FirstName { get; }
    public string LastName { get; }

    public UserCreatedIntegrationEvent(Guid userId, string email, string firstName, string lastName)
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
        SourceModule = "IdentityModule";
        UserId = userId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }
}
```

### Integration Event Handler

```csharp
public class UserCreatedIntegrationEventHandler : IIntegrationEventHandler<UserCreatedIntegrationEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<UserCreatedIntegrationEventHandler> _logger;

    public UserCreatedIntegrationEventHandler(
        IOrderRepository orderRepository,
        ILogger<UserCreatedIntegrationEventHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task Handle(UserCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling UserCreated integration event for user {UserId}", integrationEvent.UserId);

        // Cross-module business logic
        // Create welcome order or initialize customer data
        await _orderRepository.CreateWelcomeOrderAsync(integrationEvent.UserId, cancellationToken);
    }
}
```

### Key Characteristics

- **Scope**: Between modules or services
- **Purpose**: Cross-module communication and coordination
- **Source Tracking**: Includes source module information
- **Processing**: Asynchronous and eventually consistent

## Inbox Pattern

The inbox pattern ensures idempotent message processing by tracking processed messages. This prevents duplicate processing of the same message.

### Interface Definition

```csharp
public interface IInboxStore
{
    /// <summary>
    /// Stores an inbox message.
    /// </summary>
    Task StoreAsync(IInboxStore message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a message has been processed.
    /// </summary>
    Task<bool> IsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as processed.
    /// </summary>
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
}
```

### Inbox Message Model

```csharp
public class InboxMessage
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string MessageContent { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
```

### Example Usage

```csharp
public class InboxService : IInboxService
{
    private readonly IInboxStore _inboxStore;
    private readonly IMessageConsumer _messageConsumer;
    private readonly ILogger<InboxService> _logger;

    public InboxService(
        IInboxStore inboxStore,
        IMessageConsumer messageConsumer,
        ILogger<InboxService> logger)
    {
        _inboxStore = inboxStore;
        _messageConsumer = messageConsumer;
        _logger = logger;
    }

    public async Task ProcessMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var messageId = GetMessageId(message);

        // Check if message has already been processed
        if (await _inboxStore.IsProcessedAsync(messageId, cancellationToken))
        {
            _logger.LogInformation("Message {MessageId} has already been processed", messageId);
            return;
        }

        // Store message in inbox
        var inboxMessage = new InboxMessage
        {
            Id = messageId,
            MessageType = typeof(TMessage).Name,
            MessageContent = JsonSerializer.Serialize(message),
            ReceivedAt = DateTimeOffset.UtcNow
        };

        await _inboxStore.StoreAsync(inboxMessage, cancellationToken);

        try
        {
            // Process the message
            await _messageConsumer.ConsumeAsync(message, cancellationToken);

            // Mark as processed
            await _inboxStore.MarkAsProcessedAsync(messageId, cancellationToken);

            _logger.LogInformation("Successfully processed message {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message {MessageId}", messageId);
            throw;
        }
    }

    private Guid GetMessageId<TMessage>(TMessage message)
    {
        // Extract message ID based on message type
        return message switch
        {
            IIntegrationEvent integrationEvent => integrationEvent.Id,
            _ => Guid.NewGuid()
        };
    }
}
```

### Key Characteristics

- **Idempotency**: Prevents duplicate processing
- **Reliability**: Ensures message processing is tracked
- **Performance**: Fast duplicate detection
- **Storage**: Persistent message tracking

## Outbox Pattern

The outbox pattern ensures reliable message delivery by storing messages in the same database transaction as the business operation.

### Interface Definition

```csharp
public interface IOutboxStore
{
    /// <summary>
    /// Stores an outbox message.
    /// </summary>
    Task StoreAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending outbox messages.
    /// </summary>
    Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Marks an outbox message as processed.
    /// </summary>
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an outbox message as failed.
    /// </summary>
    Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default
    );
}
```

### Outbox Message Model

```csharp
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string MessageContent { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? Error { get; set; }
}
```

### Example Usage

```csharp
public class OutboxService : IOutboxService
{
    private readonly IOutboxStore _outboxStore;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<OutboxService> _logger;

    public OutboxService(
        IOutboxStore outboxStore,
        IMessagePublisher messagePublisher,
        ILogger<OutboxService> logger)
    {
        _outboxStore = outboxStore;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    public async Task AddEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(TEvent).Name,
            MessageContent = JsonSerializer.Serialize(@event),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _outboxStore.StoreAsync(outboxMessage, cancellationToken);
    }

    public async Task ProcessEventsAsync(CancellationToken cancellationToken = default)
    {
        var messages = await _outboxStore.GetPendingMessagesAsync(100, cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await ProcessMessageAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                await _outboxStore.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }
    }

    private async Task ProcessMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var eventType = Type.GetType(message.MessageType);
        if (eventType == null)
        {
            _logger.LogWarning("Unknown event type {EventType}", message.MessageType);
            return;
        }

        var eventData = JsonSerializer.Deserialize(message.MessageContent, eventType);
        if (eventData == null)
        {
            _logger.LogWarning("Failed to deserialize event data for {MessageId}", message.Id);
            return;
        }

        await _messagePublisher.PublishAsync(eventData, cancellationToken);
        await _outboxStore.MarkAsProcessedAsync(message.Id, cancellationToken);
    }
}
```

### Key Characteristics

- **Reliability**: Ensures message delivery
- **Consistency**: Same transaction as business operation
- **Retry Logic**: Built-in retry mechanism
- **Error Handling**: Failed message tracking

## Event Flow Examples

### Complete Event Flow: User Registration

```csharp
// 1. Domain Event in Identity Module
public class User : AggregateRoot<Guid>
{
    public void Create(string email, string firstName, string lastName)
    {
        // Business logic
        Email = email;
        FirstName = firstName;
        LastName = lastName;

        // Raise domain event
        AddDomainEvent(new UserCreatedEvent(Id, email, firstName, lastName));
    }
}

// 2. Domain Event Handler
public class UserCreatedDomainEventHandler : IDomainEventHandler<UserCreatedEvent>
{
    private readonly IOutboxStore _outboxStore;

    public async Task Handle(UserCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        // Internal business logic
        // Send welcome email, create user profile, etc.

        // Convert to integration event
        var integrationEvent = new UserCreatedIntegrationEvent(
            domainEvent.UserId,
            domainEvent.Email,
            domainEvent.FirstName,
            domainEvent.LastName
        );

        // Store in outbox for reliable delivery
        await _outboxStore.StoreAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(UserCreatedIntegrationEvent).Name,
            MessageContent = JsonSerializer.Serialize(integrationEvent),
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
    }
}

// 3. Integration Event Handler in Order Module
public class UserCreatedIntegrationEventHandler : IIntegrationEventHandler<UserCreatedIntegrationEvent>
{
    private readonly IInboxStore _inboxStore;
    private readonly IOrderRepository _orderRepository;

    public async Task Handle(UserCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        // Check if already processed
        if (await _inboxStore.IsProcessedAsync(integrationEvent.Id, cancellationToken))
        {
            return;
        }

        // Store in inbox
        await _inboxStore.StoreAsync(new InboxMessage
        {
            Id = integrationEvent.Id,
            MessageType = typeof(UserCreatedIntegrationEvent).Name,
            MessageContent = JsonSerializer.Serialize(integrationEvent),
            ReceivedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        // Business logic
        await _orderRepository.CreateWelcomeOrderAsync(integrationEvent.UserId, cancellationToken);

        // Mark as processed
        await _inboxStore.MarkAsProcessedAsync(integrationEvent.Id, cancellationToken);
    }
}
```

### Event Processing Pipeline

```csharp
public class EventProcessingPipeline
{
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IIntegrationEventDispatcher _integrationEventDispatcher;
    private readonly IOutboxService _outboxService;
    private readonly IInboxService _inboxService;

    public async Task ProcessDomainEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        // 1. Process domain events
        await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

        // 2. Process outbox messages
        await _outboxService.ProcessEventsAsync(cancellationToken);
    }

    public async Task ProcessIntegrationEventsAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        // 1. Process through inbox for idempotency
        await _inboxService.ProcessMessageAsync(integrationEvent, cancellationToken);

        // 2. Dispatch to handlers
        await _integrationEventDispatcher.PublishAsync(integrationEvent, cancellationToken);
    }
}
```

## Best Practices

### 1. Event Design

- **Keep events focused**: Each event should represent a single business occurrence
- **Include necessary data**: Provide all data needed by event handlers
- **Avoid sensitive data**: Don't include passwords or sensitive information
- **Version events**: Consider versioning for backward compatibility

### 2. Event Naming

```csharp
// Good: Past tense, descriptive
public class UserCreatedEvent : DomainEvent { }
public class OrderConfirmedEvent : DomainEvent { }
public class PaymentProcessedEvent : DomainEvent { }

// Bad: Present tense, vague
public class UserEvent : DomainEvent { }
public class OrderEvent : DomainEvent { }
```

### 3. Event Handler Design

```csharp
// Good: Single responsibility, focused
public class UserCreatedEventHandler : IDomainEventHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        // Single, focused responsibility
        await SendWelcomeEmailAsync(domainEvent.UserId, cancellationToken);
    }
}

// Bad: Multiple responsibilities
public class UserCreatedEventHandler : IDomainEventHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        // Too many responsibilities
        await SendWelcomeEmailAsync(domainEvent.UserId, cancellationToken);
        await CreateUserProfileAsync(domainEvent.UserId, cancellationToken);
        await NotifyAdminsAsync(domainEvent.UserId, cancellationToken);
        await UpdateAnalyticsAsync(domainEvent.UserId, cancellationToken);
    }
}
```

### 4. Error Handling

```csharp
public class ResilientEventHandler<TEvent> : IDomainEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    private readonly IRetryPolicy _retryPolicy;
    private readonly ICircuitBreaker _circuitBreaker;

    public async Task Handle(TEvent domainEvent, CancellationToken cancellationToken)
    {
        await _circuitBreaker.ExecuteAsync(async () =>
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                await ProcessEventAsync(domainEvent, cancellationToken);
            });
        });
    }
}
```

### 5. Testing Events

```csharp
[Test]
public async Task UserCreatedEvent_ShouldTriggerWelcomeEmail()
{
    // Arrange
    var user = new User(Guid.NewGuid(), "test@example.com", "John", "Doe");
    var eventHandler = new UserCreatedEventHandler(_emailService, _logger);

    // Act
    await eventHandler.Handle(new UserCreatedEvent(user.Id, user.Email, user.FirstName, user.LastName));

    // Assert
    _emailService.Verify(x => x.SendWelcomeEmailAsync(user.Id), Times.Once);
}
```

## Migration Strategies

### From Monolith to Modular Monolith

1. **Start with Domain Events**: Implement domain events within existing modules
2. **Add Integration Events**: Create integration events for cross-module communication
3. **Implement Outbox Pattern**: Add outbox for reliable message delivery
4. **Add Inbox Pattern**: Implement inbox for idempotent processing

### From Modular Monolith to Microservices

1. **Extract Integration Events**: Move integration events to message bus
2. **Replace In-Memory Events**: Use message queues (RabbitMQ, Azure Service Bus)
3. **Implement Service Discovery**: Add service discovery for inter-service communication
4. **Add API Gateway**: Implement API gateway for external communication

### Example Migration

```csharp
// Before: In-memory integration events
public class InMemoryIntegrationEventHandler : IIntegrationEventHandler<UserCreatedIntegrationEvent>
{
    public async Task Handle(UserCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        // Direct method calls
        await _orderService.CreateWelcomeOrderAsync(integrationEvent.UserId);
    }
}

// After: Message bus integration events
public class MessageBusIntegrationEventHandler : IIntegrationEventHandler<UserCreatedIntegrationEvent>
{
    private readonly IMessageBus _messageBus;

    public async Task Handle(UserCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        // Publish to message bus
        await _messageBus.PublishAsync(integrationEvent, cancellationToken);
    }
}
```

## Conclusion

The event system in Raziee.SharedKernel provides a comprehensive foundation for building event-driven architectures. By understanding and properly implementing Domain Events, Integration Events, Inbox Pattern, and Outbox Pattern, you can create robust, scalable, and maintainable applications that can evolve from modular monoliths to microservices.

Remember to:
- Use Domain Events for internal business logic
- Use Integration Events for cross-module communication
- Implement Inbox Pattern for idempotent processing
- Implement Outbox Pattern for reliable message delivery
- Follow best practices for event design and handling
- Plan for migration from monolith to microservices
