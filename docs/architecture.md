# Architecture Guide

This document provides an overview of the architecture and design decisions behind Raziee.SharedKernel.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Design Principles](#design-principles)
- [Core Components](#core-components)
- [Patterns and Practices](#patterns-and-practices)
- [Layered Architecture](#layered-architecture)
- [Modular Monolith Support](#modular-monolith-support)
- [Microservices Abstractions](#microservices-abstractions)
- [Performance Considerations](#performance-considerations)
- [Security Considerations](#security-considerations)

## Architecture Overview

Raziee.SharedKernel follows Clean Architecture principles and provides a comprehensive foundation for building domain-driven applications. The library is designed to be:

- **Modular**: Components can be used independently
- **Extensible**: Easy to extend and customize
- **Testable**: Built with testability in mind
- **Performant**: Optimized for performance
- **Maintainable**: Clean, readable, and well-documented code

## Design Principles

### 1. Domain-Driven Design (DDD)

The library is built around DDD principles:

- **Entities**: Represent objects with identity
- **Value Objects**: Represent concepts without identity
- **Aggregates**: Consistency boundaries
- **Domain Events**: Communication between aggregates
- **Repositories**: Data access abstraction

### 2. Separation of Concerns

Each component has a single responsibility:

- **Domain Layer**: Business logic and rules
- **Application Layer**: Use cases and orchestration
- **Infrastructure Layer**: External concerns
- **Presentation Layer**: User interface

### 3. Dependency Inversion

High-level modules don't depend on low-level modules. Both depend on abstractions.

### 4. Open/Closed Principle

The library is open for extension but closed for modification.

## Core Components

### Domain Building Blocks

#### Entities

```csharp
public abstract class Entity<TId> : IEquatable<Entity<TId>>
{
    public TId Id { get; protected set; }
    // Equality based on ID
}

public abstract class AggregateRoot<TId> : Entity<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void AddDomainEvent(IDomainEvent domainEvent);
    public void ClearDomainEvents();
}
```

#### Value Objects

```csharp
public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object> GetEqualityComponents();
    // Structural equality based on components
}
```

#### Domain Events

```csharp
public interface IDomainEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredOn { get; }
    int Version { get; }
}
```

### Repository Pattern

#### Generic Repository

```csharp
public interface IRepository<TEntity, TId> : IReadRepository<TEntity, TId>
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
```

#### Specification Pattern

```csharp
public abstract class BaseSpecification<TEntity, TId> : ISpecification<TEntity>
{
    public Expression<Func<TEntity, bool>>? Criteria { get; private set; }
    public List<Expression<Func<TEntity, object>>> Includes { get; } = new();
    public Expression<Func<TEntity, object>>? OrderBy { get; private set; }
    // ... other properties
}
```

### CQRS Support

#### Commands and Queries

```csharp
public interface ICommand : IRequest { }
public interface ICommand<TResponse> : IRequest<TResponse> { }
public interface IQuery<TResponse> : IRequest<TResponse> { }
```

#### Pipeline Behaviors

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    // Automatic validation with FluentValidation
}

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    // Automatic transaction management
}
```

## Patterns and Practices

### 1. Unit of Work Pattern

Ensures data consistency across multiple operations:

```csharp
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

### 2. Domain Events Pattern

Enables loose coupling between aggregates:

```csharp
public class DomainEventDispatcher : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        // Dispatch events to registered handlers
    }
}
```

### 3. Specification Pattern

Encapsulates business rules for data access:

```csharp
public class UserByEmailSpecification : BaseSpecification<User, Guid>
{
    public UserByEmailSpecification(string email)
    {
        AddCriteria(u => u.Email == email);
    }
}
```

### 4. Factory Pattern

For creating complex objects:

```csharp
public class UserFactory
{
    public User CreateUser(string name, string email)
    {
        return new User(Guid.NewGuid(), name, email);
    }
}
```

## Layered Architecture

### Domain Layer

Contains business logic and rules:

- **Entities**: `User`, `Product`, `Order`
- **Value Objects**: `Email`, `Address`, `Money`
- **Domain Events**: `UserCreatedEvent`, `OrderPlacedEvent`
- **Domain Services**: `UserDomainService`, `OrderDomainService`

### Application Layer

Contains use cases and orchestration:

- **Commands**: `CreateUserCommand`, `UpdateUserCommand`
- **Queries**: `GetUserByIdQuery`, `GetUsersQuery`
- **Handlers**: `CreateUserCommandHandler`, `GetUserByIdQueryHandler`
- **Application Services**: `UserApplicationService`

### Infrastructure Layer

Contains external concerns:

- **Repositories**: `EfUserRepository`, `MongoUserRepository`
- **External Services**: `EmailService`, `PaymentService`
- **Data Access**: `DbContext`, `MongoContext`

### Presentation Layer

Contains user interface:

- **Controllers**: `UsersController`, `OrdersController`
- **ViewModels**: `UserViewModel`, `OrderViewModel`
- **API Endpoints**: RESTful APIs, GraphQL

## Modular Monolith Support

### Module Abstractions

```csharp
public interface IModule
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    IEnumerable<string> Dependencies { get; }
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}
```

### Integration Events

```csharp
public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredOn { get; }
    string SourceModule { get; }
}
```

### Module Communication

```csharp
public interface IModuleCommunication
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
    Task SubscribeAsync<TEvent>(Func<TEvent, CancellationToken, Task> handler, CancellationToken cancellationToken = default);
    Task SendAsync<TMessage>(string targetModule, TMessage message, CancellationToken cancellationToken = default);
}
```

## Microservices Abstractions

### Messaging Abstractions

```csharp
public interface IMessageBus
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default);
    Task SubscribeAsync<TMessage>(Func<TMessage, CancellationToken, Task> handler, CancellationToken cancellationToken = default);
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
```

### Outbox/Inbox Pattern

```csharp
public interface IOutboxStore
{
    Task StoreAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(int batchSize = 100, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
}
```

### Saga Pattern

```csharp
public interface ISagaOrchestrator
{
    Task StartSagaAsync<TData>(Guid sagaId, TData data, CancellationToken cancellationToken = default);
    Task ExecuteNextStepAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default);
    Task CompensateStepAsync<TData>(Guid sagaId, int stepIndex, CancellationToken cancellationToken = default);
}
```

### Circuit Breaker Pattern

```csharp
public interface ICircuitBreaker
{
    string Name { get; }
    CircuitBreakerState State { get; }
    Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default);
}
```

## Performance Considerations

### 1. Lazy Loading

Use lazy loading for related entities:

```csharp
public class User : AggregateRoot<Guid>
{
    private readonly Lazy<List<Order>> _orders;
    
    public User(Guid id, string name, string email) : base(id)
    {
        Name = name;
        Email = email;
        _orders = new Lazy<List<Order>>(() => LoadOrders());
    }
    
    public IReadOnlyList<Order> Orders => _orders.Value;
}
```

### 2. Caching

Implement caching for frequently accessed data:

```csharp
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IMemoryCache _cache;
    
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var cacheKey = GenerateCacheKey(request);
        
        if (_cache.TryGetValue(cacheKey, out TResponse? cachedResponse))
            return cachedResponse!;
        
        var response = await next();
        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));
        
        return response;
    }
}
```

### 3. Async/Await

Use async/await for I/O operations:

```csharp
public async Task<User> GetUserAsync(Guid userId)
{
    return await _userRepository.GetByIdAsync(userId);
}
```

### 4. Pagination

Implement pagination for large datasets:

```csharp
public async Task<PaginatedResult<User>> GetUsersAsync(int pageNumber, int pageSize)
{
    return await _userRepository.GetPagedAsync(pageNumber, pageSize);
}
```

## Security Considerations

### 1. Input Validation

Validate all inputs:

```csharp
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
        
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
```

### 2. Authorization

Implement proper authorization:

```csharp
[Authorize]
public class UsersController : ControllerBase
{
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteUser(Guid id)
    {
        // Delete user logic
    }
}
```

### 3. Data Protection

Protect sensitive data:

```csharp
public class User : AggregateRoot<Guid>
{
    private string _passwordHash;
    
    public string PasswordHash
    {
        get => _passwordHash;
        private set => _passwordHash = HashPassword(value);
    }
}
```

### 4. Audit Logging

Log important operations:

```csharp
public class AuditLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing {RequestType}", typeof(TRequest).Name);
        
        var response = await next();
        
        _logger.LogInformation("Completed {RequestType}", typeof(TRequest).Name);
        
        return response;
    }
}
```

## Best Practices

### 1. Domain Modeling

- Keep aggregates small and focused
- Use value objects for concepts without identity
- Raise domain events for important business events
- Keep business logic in the domain layer

### 2. Repository Pattern

- Use generic repositories for common operations
- Implement specifications for complex queries
- Use unit of work for transaction management
- Keep repositories focused on data access

### 3. CQRS

- Separate commands from queries
- Use handlers for business logic
- Implement pipeline behaviors for cross-cutting concerns
- Keep handlers focused and testable

### 4. Testing

- Write unit tests for domain logic
- Write integration tests for data access
- Use test doubles for external dependencies
- Maintain high test coverage

### 5. Documentation

- Document public APIs
- Provide code examples
- Keep documentation up to date
- Use meaningful names and comments

## Conclusion

Raziee.SharedKernel provides a solid foundation for building domain-driven applications. By following the patterns and practices outlined in this guide, you can create maintainable, testable, and scalable applications.

For more information, see:

- [Getting Started Guide](getting-started.md)
- [API Reference](api-reference.md)
- [Examples](examples/)
- [GitHub Repository](https://github.com/raziee/Raziee.SharedKernel)
