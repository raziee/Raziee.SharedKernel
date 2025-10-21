# Raziee.SharedKernel

[![Build Status](https://github.com/raziee/Raziee.SharedKernel/workflows/CI/badge.svg)](https://github.com/raziee/Raziee.SharedKernel/actions)
[![Coverage](https://codecov.io/gh/raziee/Raziee.SharedKernel/branch/main/graph/badge.svg)](https://codecov.io/gh/raziee/Raziee.SharedKernel)
[![NuGet Version](https://img.shields.io/nuget/v/Raziee.SharedKernel.svg)](https://www.nuget.org/packages/Raziee.SharedKernel)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A robust and comprehensive Domain-Driven Design (DDD) foundation library for .NET applications, providing essential building blocks for creating well-structured domain layers in complex business applications.

## Introduction

Raziee.SharedKernel is a comprehensive and powerful library for implementing Domain-Driven Design (DDD), CQRS, and Multi-Tenancy patterns in .NET applications. This library provides foundational components and standard patterns to facilitate the development of complex and scalable applications.

## Key Features

### 🏗️ DDD Building Blocks
- **Entity Base Classes**: `Entity<TId>`, `AggregateRoot<TId>`, and `AuditableEntity<TId>` base classes
- **Value Objects**: Base `ValueObject` class with advanced equality comparison
- **Soft Delete**: Support for soft deletion with automatic query filters

### 🎯 Domain-Driven Design
- **Domain Events**: Domain event system with `DomainEvent` and `IDomainEventDispatcher`
- **Integration Events**: Cross-module communication with `IIntegrationEvent` and `IIntegrationEventDispatcher`
- **Domain Exceptions**: Domain-specific exceptions including `DomainException`, `DomainValidationException`, `EntityNotFoundException`, and `ConcurrencyException`
- **Aggregate Root**: Aggregate Root pattern with automatic domain event management

### 🗄️ Repository Pattern
- **Generic Repositories**: Implementation of `IRepository<T>` and `EfRepository<T>`
- **Specification Pattern**: `BaseSpecification<TEntity, TId>` class for complex queries
- **Unit of Work**: Transaction management with `IUnitOfWork` and `UnitOfWork`

### ⚡ CQRS Support
- **MediatR Integration**: Full integration with MediatR
- **Pipeline Behaviors**:
  - `ValidationBehavior`: Automatic validation with FluentValidation
  - `TransactionBehavior`: Automatic transaction management
  - `LoggingBehavior`: Automatic request/response logging
  - `CachingBehavior`: Response caching support

### 🗃️ Data Access
- **DbContextBase**: Base class with automatic domain event dispatching
- **Model Builder Extensions**: Entity Framework model builder extensions
- **Transaction Management**: Full support for transaction management

### 🏢 Multi-Tenancy
- **Multi-Tenant Data Isolation**: Multi-tenant data isolation support
- **Tenant-Aware Entities**: Tenant-aware entity support

### 📨 Event-Driven Architecture
- **Inbox Pattern**: Idempotent message processing with `IInboxStore`
- **Outbox Pattern**: Reliable message delivery with `IOutboxStore`
- **Event Bus**: In-memory and message bus event publishing
- **Message Processing**: Reliable message processing with retry and error handling

### 🏗️ Vertical Slice Architecture
- **IFeature Interface**: Feature definition and management
- **FeatureBase Class**: Base implementation with lifecycle management
- **Feature Management**: Registration, initialization, and shutdown
- **Feature Configuration**: Dynamic feature configuration and toggling

### 🔧 Dependency Injection
- **Fluent Builder Pattern**: `SharedKernelBuilder` pattern for easy configuration
- **Extension Methods**: Extension methods for service registration

### 🏗️ Modular Monolith Support
- **Module Abstractions**: `IModule` interface and `ModuleRegistry` for module management
- **Integration Events**: `IIntegrationEvent` and `IntegrationEventDispatcher` for inter-module communication
- **InMemoryEventBus**: In-memory event bus for modular monolith architectures

### 🚀 Microservices Abstractions
- **Messaging Abstractions**: `IMessageBus`, `IMessagePublisher`, `IMessageConsumer` interfaces
- **Outbox/Inbox Pattern**: `IOutboxStore` and `IInboxStore` for reliable messaging
- **Saga Pattern**: `ISagaOrchestrator` and `SagaStep<TData>` for distributed transactions
- **Circuit Breaker**: `ICircuitBreaker` and `IRetryPolicy` for resilient service communication

## Installation

```bash
dotnet add package Raziee.SharedKernel
```

## Quick Start

### 1. Configure Services

```csharp
using Raziee.SharedKernel.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add SharedKernel with default configuration
builder.Services.AddSharedKernel();

// Or configure specific features
builder.Services.AddSharedKernel(sharedKernel =>
{
    sharedKernel
        .AddDomainEvents()
        .AddCQRS()
        .AddRepositories()
        .AddUnitOfWork()
        .AddMultiTenancy()
        .AddModules()
        .AddIntegrationEvents()
        .AddMessaging()
        .AddDistributedTransactions()
        .AddServiceCommunication()
        .AddCaching()
        .AddLogging();
});
```

### 2. Create Domain Entities

```csharp
using Raziee.SharedKernel.Domain.Entities;
using Raziee.SharedKernel.Domain.Events;

public class User : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Email { get; private set; }

    public User(Guid id, string name, string email) : base(id)
    {
        Name = name;
        Email = email;
        
        // Raise domain event
        AddDomainEvent(new UserCreatedEvent(Id, Name, Email));
    }
}

public class UserCreatedEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Name { get; }
    public string Email { get; }

    public UserCreatedEvent(Guid userId, string name, string email)
    {
        UserId = userId;
        Name = name;
        Email = email;
    }
}
```

### 3. Create Value Objects

```csharp
using Raziee.SharedKernel.Domain.ValueObjects;

public class Email : ValueObject
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.Contains("@"))
            throw new ArgumentException("Invalid email format", nameof(value));
        
        Value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

### 4. Create Commands and Queries

```csharp
using Raziee.SharedKernel.CQRS;

public class CreateUserCommand : ICommand<CreateUserResult>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CreateUserResult
{
    public Guid UserId { get; set; }
}

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, CreateUserResult>
{
    private readonly IRepository<User, Guid> _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(IRepository<User, Guid> userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateUserResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User(Guid.NewGuid(), request.Name, request.Email);
        
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return new CreateUserResult { UserId = user.Id };
    }
}
```

### 5. Create Specifications

```csharp
using Raziee.SharedKernel.Specifications;

public class UserByEmailSpecification : BaseSpecification<User, Guid>
{
    public UserByEmailSpecification(string email)
    {
        AddCriteria(u => u.Email == email);
    }
}
```

## Architecture

Raziee.SharedKernel follows Clean Architecture principles and provides:

- **Domain Layer**: Entities, Value Objects, Domain Events, and Domain Exceptions
- **Application Layer**: CQRS, Pipeline Behaviors, and Application Services
- **Infrastructure Layer**: Repository implementations, Data Access, and External Services
- **Presentation Layer**: Controllers, API endpoints, and UI components

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Documentation

### 📚 Core Documentation
- 📖 [Getting Started Guide](docs/getting-started.md) - [فارسی](docs/getting-started-fa.md)
- 🏗️ [Architecture Overview](docs/architecture.md) - [فارسی](docs/architecture-fa.md)

### 🎯 Pattern Guides
- 🎯 [Domain-Driven Design Guide](docs/guides/ddd-guide.md) - [فارسی](docs/guides/ddd-guide-fa.md)
- ⚡ [CQRS Guide](docs/guides/cqrs-guide.md) - [فارسی](docs/guides/cqrs-guide-fa.md)
- 🗄️ [Repository Pattern Guide](docs/guides/repository-pattern-guide.md) - [فارسی](docs/guides/repository-pattern-guide-fa.md)
- 🏢 [Multi-Tenancy Guide](docs/guides/multitenancy-guide.md) - [فارسی](docs/guides/multitenancy-guide-fa.md)

### 🏗️ Architecture Guides
- 🏢 [Modular Monolith Guide](docs/guides/modular-monolith-guide.md) - [فارسی](docs/guides/modular-monolith-guide-fa.md)
- 🚀 [Microservices Guide](docs/guides/microservices-guide.md) - [فارسی](docs/guides/microservices-guide-fa.md)
- 🏗️ [Vertical Slice Architecture Guide](docs/guides/vertical-slice-architecture-guide.md) - [فارسی](docs/guides/vertical-slice-architecture-guide-fa.md)

### 📨 Communication & Events
- 📨 [Event System Guide](docs/guides/event-system-guide.md) - [فارسی](docs/guides/event-system-guide-fa.md)
- 📨 [Messaging Patterns Guide](docs/guides/messaging-patterns-guide.md) - [فارسی](docs/guides/messaging-patterns-guide-fa.md)
- 🔧 [Service Communication Guide](docs/guides/service-communication-guide.md) - [فارسی](docs/guides/service-communication-guide-fa.md)

### 🔄 Advanced Patterns
- 🔄 [Distributed Transactions Guide](docs/guides/distributed-transactions-guide.md) - [فارسی](docs/guides/distributed-transactions-guide-fa.md)
- 🗃️ [Entity Framework Extensions Guide](docs/guides/entity-framework-extensions-guide.md) - [فارسی](docs/guides/entity-framework-extensions-guide-fa.md)

### 📖 All Guides
- 📚 [Complete Guides Index](docs/guides/README.md) - [فارسی](docs/guides/README-fa.md)

## Support

- 🐛 [Issue Tracker](https://github.com/raziee/Raziee.SharedKernel/issues)
- 💬 [Discussions](https://github.com/raziee/Raziee.SharedKernel/discussions)

---

**Raziee.SharedKernel** - Building robust, scalable, and maintainable .NET applications with Domain-Driven Design principles.
