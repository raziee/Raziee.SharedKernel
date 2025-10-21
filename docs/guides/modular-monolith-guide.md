# Modular Monolith Guide

This comprehensive guide demonstrates how to use Raziee.SharedKernel to build a modular monolith architecture that can evolve into microservices.

## Table of Contents

- [Introduction](#introduction)
- [Modular Monolith Architecture](#modular-monolith-architecture)
- [Module Design Principles](#module-design-principles)
- [Module Communication](#module-communication)
- [Integration Events](#integration-events)
- [Complete Example: E-Commerce Platform](#complete-example-e-commerce-platform)
- [Migration to Microservices](#migration-to-microservices)
- [Best Practices](#best-practices)

## Introduction

A modular monolith is a single deployable application composed of loosely coupled modules. Each module represents a business capability and can be developed, tested, and maintained independently while sharing the same database and deployment unit.

## Modular Monolith Architecture

### Architecture Overview

```
┌────────────────────────────────────────────────────────────┐
│                    Modular Monolith                        │
├────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │   User      │  │  Product    │  │   Order     │         │
│  │   Module    │  │   Module    │  │   Module    │         │
│  │             │  │             │  │             │         │
│  │ • Domain    │  │ • Domain    │  │ • Domain    │         │
│  │ • App       │  │ • App       │  │ • App       │         │
│  │ • Infra     │  │ • Infra     │  │ • Infra     │         │
│  │ • API       │  │ • API       │  │ • API       │         │
│  └─────────────┘  └─────────────┘  └─────────────┘         │
│         │               │               │                  │
│         └───────────────┼───────────────┘                  │
│                         │                                  │
│  ┌────────────────────────────────────────────────────┐    │
│  │              Integration Events                    │    │
│  │         (In-Memory Event Bus)                      │    │
│  │                                                    │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │    │
│  │  │   Event     │  │   Event     │  │   Event     │ │    │
│  │  │   Bus       │  │   Store     │  │   Handlers  │ │    │
│  │  │             │  │             │  │             │ │    │
│  │  │ • Publish   │  │ • In-Memory │  │ • Async     │ │    │
│  │  │ • Subscribe │  │ • Reliable  │  │ • Sync      │ │    │
│  │  │ • Route     │  │ • Fast      │  │ • Error     │ │    │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │    │
│  └────────────────────────────────────────────────────┘    │
│                         │                                  │
│  ┌────────────────────────────────────────────────────┐    │
│  │              Shared Database                       │    │
│  │                                                    │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │    │
│  │  │   User      │  │  Product    │  │   Order     │ │    │
│  │  │   Tables    │  │   Tables    │  │   Tables    │ │    │
│  │  │             │  │             │  │             │ │    │
│  │  │ • Users     │  │ • Products  │  │ • Orders    │ │    │
│  │  │ • Profiles  │  │ • Categories│  │ • Items     │ │    │
│  │  │ • Roles     │  │ • Inventory │  │ • Payments  │ │    │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │    │
│  └────────────────────────────────────────────────────┘    │
└────────────────────────────────────────────────────────────┘
```

## Module Design Principles

### 1. Module Definition

Each module should be:
- **Self-contained**: Has its own domain logic
- **Loosely coupled**: Minimal dependencies on other modules
- **Highly cohesive**: Related functionality grouped together
- **Independently testable**: Can be tested in isolation

### 2. Module Structure

```
src/
├── Modules/
│   ├── UserModule/
│   │   ├── Domain/
│   │   │   ├── Entities/
│   │   │   ├── ValueObjects/
│   │   │   ├── Events/
│   │   │   └── Services/
│   │   ├── Application/
│   │   │   ├── Commands/
│   │   │   ├── Queries/
│   │   │   └── Handlers/
│   │   ├── Infrastructure/
│   │   │   ├── Repositories/
│   │   │   └── Services/
│   │   └── Presentation/
│   │       └── Controllers/
│   ├── ProductModule/
│   └── OrderModule/
└── Shared/
    ├── Kernel/
    └── Infrastructure/
```

## Module Communication

### 1. Module Interface

```csharp
using Raziee.SharedKernel.Modules;

public interface IUserModule : IModule
{
    string Name => "UserModule";
    string Version => "1.0.0";
    string Description => "User management module";
    IEnumerable<string> Dependencies => new[] { "SharedKernel" };
}

public class UserModule : IUserModule
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserModule> _logger;

    public UserModule(IServiceProvider serviceProvider, ILogger<UserModule> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing UserModule");
        // Module initialization logic
        await Task.CompletedTask;
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Shutting down UserModule");
        // Module cleanup logic
        await Task.CompletedTask;
    }
}
```

### 2. Module Communication Service

```csharp
using Raziee.SharedKernel.Modules;

public interface IUserModuleCommunication
{
    Task<UserDto> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsUserActiveAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserProfileDto> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class UserModuleCommunication : IUserModuleCommunication
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserModuleCommunication> _logger;

    public UserModuleCommunication(IUserRepository userRepository, ILogger<UserModuleCommunication> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<UserDto> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting user {UserId} from UserModule", userId);
        
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null) return null;

        return new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email.Value,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> IsUserActiveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user?.IsActive ?? false;
    }

    public async Task<UserProfileDto> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null) return null;

        return new UserProfileDto
        {
            Id = user.Id,
            FullName = $"{user.FirstName} {user.LastName}",
            Email = user.Email.Value,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };
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

## Integration Events

### 1. Integration Event Definition

```csharp
using Raziee.SharedKernel.Modules.Events;

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
        SourceModule = "UserModule";
        UserId = userId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }
}

public class UserDeactivatedIntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; }
    public DateTimeOffset OccurredOn { get; }
    public string SourceModule { get; }
    public Guid UserId { get; }
    public string Reason { get; }

    public UserDeactivatedIntegrationEvent(Guid userId, string reason)
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
        SourceModule = "UserModule";
        UserId = userId;
        Reason = reason;
    }
}

public class ProductCreatedIntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; }
    public DateTimeOffset OccurredOn { get; }
    public string SourceModule { get; }
    public Guid ProductId { get; }
    public string Name { get; }
    public decimal Price { get; }
    public string Currency { get; }

    public ProductCreatedIntegrationEvent(Guid productId, string name, decimal price, string currency)
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
        SourceModule = "ProductModule";
        ProductId = productId;
        Name = name;
        Price = price;
        Currency = currency;
    }
}

public class OrderCreatedIntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; }
    public DateTimeOffset OccurredOn { get; }
    public string SourceModule { get; }
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public decimal TotalAmount { get; }
    public string Currency { get; }

    public OrderCreatedIntegrationEvent(Guid orderId, Guid customerId, decimal totalAmount, string currency)
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
        SourceModule = "OrderModule";
        OrderId = orderId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
        Currency = currency;
    }
}
```

### 2. Integration Event Handlers

```csharp
// OrderModule handling UserCreatedIntegrationEvent
public class UserCreatedIntegrationEventHandler : IIntegrationEventHandler<UserCreatedIntegrationEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<UserCreatedIntegrationEventHandler> _logger;

    public UserCreatedIntegrationEventHandler(IOrderRepository orderRepository, ILogger<UserCreatedIntegrationEventHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task Handle(UserCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling UserCreated event for user {UserId}", integrationEvent.UserId);

        // Create a welcome order or initialize customer data
        // This is an example of cross-module communication
        await Task.CompletedTask;
    }
}

// ProductModule handling OrderCreatedIntegrationEvent
public class OrderCreatedIntegrationEventHandler : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<OrderCreatedIntegrationEventHandler> _logger;

    public OrderCreatedIntegrationEventHandler(IProductRepository productRepository, ILogger<OrderCreatedIntegrationEventHandler> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task Handle(OrderCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling OrderCreated event for order {OrderId}", integrationEvent.OrderId);

        // Update product statistics or inventory
        // This is an example of cross-module communication
        await Task.CompletedTask;
    }
}

// UserModule handling ProductCreatedIntegrationEvent
public class ProductCreatedIntegrationEventHandler : IIntegrationEventHandler<ProductCreatedIntegrationEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ProductCreatedIntegrationEventHandler> _logger;

    public ProductCreatedIntegrationEventHandler(IUserRepository userRepository, ILogger<ProductCreatedIntegrationEventHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task Handle(ProductCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling ProductCreated event for product {ProductId}", integrationEvent.ProductId);

        // Notify users about new products or update recommendations
        // This is an example of cross-module communication
        await Task.CompletedTask;
    }
}
```

## Complete Example: E-Commerce Platform

### 1. User Module

```csharp
// UserModule/Domain/Entities/User.cs
public class User : AggregateRoot<Guid>
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Email Email { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public User(Guid id, string firstName, string lastName, Email email) : base(id)
    {
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new UserCreatedEvent(Id, Email.Value, FirstName, LastName));
    }

    public void UpdateName(string firstName, string lastName)
    {
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
    }

    public void ChangeEmail(Email newEmail)
    {
        Email = newEmail ?? throw new ArgumentNullException(nameof(newEmail));
    }

    public void Deactivate(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required", nameof(reason));

        IsActive = false;
        AddDomainEvent(new UserDeactivatedEvent(Id, reason));
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
    }
}

// UserModule/Domain/Events/UserCreatedEvent.cs
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

// UserModule/Domain/Events/UserDeactivatedEvent.cs
public class UserDeactivatedEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Reason { get; }

    public UserDeactivatedEvent(Guid userId, string reason)
    {
        UserId = userId;
        Reason = reason;
    }
}
```

### 2. Product Module

```csharp
// ProductModule/Domain/Entities/Product.cs
public class Product : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Money Price { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Product(Guid id, string name, string description, Money price, int stockQuantity) : base(id)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Price = price ?? throw new ArgumentNullException(nameof(price));
        StockQuantity = stockQuantity;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new ProductCreatedEvent(Id, Name, Price.Amount, Price.Currency));
    }

    public void UpdatePrice(Money newPrice)
    {
        if (newPrice.Amount < 0)
            throw new ArgumentException("Price cannot be negative", nameof(newPrice));

        Price = newPrice;
    }

    public void UpdateStock(int newQuantity)
    {
        if (newQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative", nameof(newQuantity));

        StockQuantity = newQuantity;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

// ProductModule/Domain/Events/ProductCreatedEvent.cs
public class ProductCreatedEvent : DomainEvent
{
    public Guid ProductId { get; }
    public string Name { get; }
    public decimal Price { get; }
    public string Currency { get; }

    public ProductCreatedEvent(Guid productId, string name, decimal price, string currency)
    {
        ProductId = productId;
        Name = name;
        Price = price;
        Currency = currency;
    }
}
```

### 3. Order Module

```csharp
// OrderModule/Domain/Entities/Order.cs
public class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = new();

    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public Order(Guid id, Guid customerId) : base(id)
    {
        CustomerId = customerId;
        Status = OrderStatus.Pending;
        TotalAmount = new Money(0, "USD");
        CreatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new OrderCreatedEvent(Id, CustomerId, TotalAmount.Amount, TotalAmount.Currency));
    }

    public void AddItem(Product product, int quantity)
    {
        if (product == null) throw new ArgumentNullException(nameof(product));
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive", nameof(quantity));
        if (Status != OrderStatus.Pending) throw new InvalidOperationException("Cannot modify completed order");

        var existingItem = _items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existingItem != null)
        {
            existingItem.IncreaseQuantity(quantity);
        }
        else
        {
            var newItem = new OrderItem(product.Id, product.Name, product.Price, quantity);
            _items.Add(newItem);
        }

        RecalculateTotal();
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending) throw new InvalidOperationException("Only pending orders can be confirmed");
        if (!_items.Any()) throw new InvalidOperationException("Cannot confirm empty order");

        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id, CustomerId, TotalAmount.Amount, TotalAmount.Currency));
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Aggregate(
            new Money(0, "USD"),
            (total, item) => total.Add(item.TotalPrice)
        );
    }
}

// OrderModule/Domain/Events/OrderCreatedEvent.cs
public class OrderCreatedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public decimal TotalAmount { get; }
    public string Currency { get; }

    public OrderCreatedEvent(Guid orderId, Guid customerId, decimal totalAmount, string currency)
    {
        OrderId = orderId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
        Currency = currency;
    }
}

// OrderModule/Domain/Events/OrderConfirmedEvent.cs
public class OrderConfirmedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public decimal TotalAmount { get; }
    public string Currency { get; }

    public OrderConfirmedEvent(Guid orderId, Guid customerId, decimal totalAmount, string currency)
    {
        OrderId = orderId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
        Currency = currency;
    }
}
```

### 4. Module Registration

```csharp
// Program.cs or Startup.cs
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add Raziee.SharedKernel
        builder.Services.AddSharedKernel();

        // Register modules
        builder.Services.AddUserModule();
        builder.Services.AddProductModule();
        builder.Services.AddOrderModule();

        // Register integration event handlers
        builder.Services.AddIntegrationEventHandlers();

        var app = builder.Build();

        // Initialize modules
        app.Services.InitializeModules();

        app.Run();
    }
}

// Extension methods for module registration
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserModule(this IServiceCollection services)
    {
        // Register UserModule services
        services.AddScoped<IUserModule, UserModule>();
        services.AddScoped<IUserModuleCommunication, UserModuleCommunication>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IUserDomainService, UserDomainService>();

        return services;
    }

    public static IServiceCollection AddProductModule(this IServiceCollection services)
    {
        // Register ProductModule services
        services.AddScoped<IProductModule, ProductModule>();
        services.AddScoped<IProductModuleCommunication, ProductModuleCommunication>();
        services.AddScoped<IProductRepository, EfProductRepository>();
        services.AddScoped<IProductDomainService, ProductDomainService>();

        return services;
    }

    public static IServiceCollection AddOrderModule(this IServiceCollection services)
    {
        // Register OrderModule services
        services.AddScoped<IOrderModule, OrderModule>();
        services.AddScoped<IOrderModuleCommunication, OrderModuleCommunication>();
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddScoped<IOrderDomainService, OrderDomainService>();

        return services;
    }

    public static IServiceCollection AddIntegrationEventHandlers(this IServiceCollection services)
    {
        // Register integration event handlers
        services.AddScoped<IIntegrationEventHandler<UserCreatedIntegrationEvent>, UserCreatedIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<ProductCreatedIntegrationEvent>, ProductCreatedIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<OrderCreatedIntegrationEvent>, OrderCreatedIntegrationEventHandler>();

        return services;
    }
}

// Module initialization
public static class ModuleExtensions
{
    public static async Task InitializeModules(this IServiceProvider serviceProvider)
    {
        var modules = serviceProvider.GetServices<IModule>();
        
        foreach (var module in modules)
        {
            await module.InitializeAsync();
        }
    }
}
```

## Migration to Microservices

### 1. Preparation Phase

```csharp
// Create module boundaries
public interface IModuleBoundary
{
    string ModuleName { get; }
    IEnumerable<Type> DomainTypes { get; }
    IEnumerable<Type> ApplicationTypes { get; }
    IEnumerable<Type> InfrastructureTypes { get; }
}

public class UserModuleBoundary : IModuleBoundary
{
    public string ModuleName => "UserModule";
    
    public IEnumerable<Type> DomainTypes => new[]
    {
        typeof(User),
        typeof(Email),
        typeof(UserCreatedEvent),
        typeof(UserDeactivatedEvent)
    };

    public IEnumerable<Type> ApplicationTypes => new[]
    {
        typeof(CreateUserCommand),
        typeof(CreateUserCommandHandler),
        typeof(GetUserQuery),
        typeof(GetUserQueryHandler)
    };

    public IEnumerable<Type> InfrastructureTypes => new[]
    {
        typeof(EfUserRepository),
        typeof(UserModuleCommunication)
    };
}
```

### 2. Database Separation

```csharp
// Separate database contexts
public class UserDbContext : DbContextBase
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // User module specific configuration
    }
}

public class ProductDbContext : DbContextBase
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Product module specific configuration
    }
}

public class OrderDbContext : DbContextBase
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Order module specific configuration
    }
}
```

### 3. Message Bus Integration

```csharp
// Replace in-memory events with message bus
public class UserCreatedIntegrationEventHandler : IIntegrationEventHandler<UserCreatedIntegrationEvent>
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<UserCreatedIntegrationEventHandler> _logger;

    public UserCreatedIntegrationEventHandler(IMessageBus messageBus, ILogger<UserCreatedIntegrationEventHandler> logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }

    public async Task Handle(UserCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing UserCreated event to message bus");

        // Publish to message bus instead of in-memory
        await _messageBus.PublishAsync(integrationEvent, cancellationToken);
    }
}
```

## Best Practices

### 1. Module Design
- Keep modules focused on a single business capability
- Minimize dependencies between modules
- Use integration events for cross-module communication
- Design for eventual microservice extraction

### 2. Communication Patterns
- Use integration events for loose coupling
- Implement module communication interfaces
- Avoid direct database access between modules
- Use message queues for async communication

### 3. Data Management
- Use shared database initially
- Design for database separation
- Implement proper transaction boundaries
- Use eventual consistency where appropriate

### 4. Testing Strategy
- Test modules in isolation
- Use integration tests for cross-module scenarios
- Mock external dependencies
- Implement contract testing

### 5. Deployment Considerations
- Deploy as a single unit initially
- Plan for module extraction
- Use feature flags for module activation
- Monitor module performance independently

This guide provides a comprehensive foundation for building modular monoliths with Raziee.SharedKernel that can evolve into microservices when needed.
