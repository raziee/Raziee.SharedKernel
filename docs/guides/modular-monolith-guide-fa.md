# راهنمای Modular Monolith

این راهنمای جامع نحوه استفاده از Raziee.SharedKernel برای ساخت معماری modular monolith که می‌تواند به microservices تکامل یابد را نشان می‌دهد.

## فهرست مطالب

- [مقدمه](#مقدمه)
- [معماری Modular Monolith](#معماری-modular-monolith)
- [اصول طراحی ماژول](#اصول-طراحی-ماژول)
- [ارتباط ماژول](#ارتباط-ماژول)
- [رویدادهای ادغام](#رویدادهای-ادغام)
- [مثال کامل: پلتفرم تجارت الکترونیک](#مثال-کامل-پلتفرم-تجارت-الکترونیک)
- [مهاجرت به Microservices](#مهاجرت-به-microservices)
- [بهترین شیوه‌ها](#بهترین-شیوه‌ها)

## مقدمه

یک modular monolith یک برنامه قابل استقرار واحد است که از ماژول‌های loosely coupled تشکیل شده است. هر ماژول یک قابلیت کسب‌وکار را نشان می‌دهد و می‌تواند به طور مستقل توسعه، تست و نگهداری شود در حالی که همان پایگاه داده و واحد استقرار را به اشتراک می‌گذارد.

## معماری Modular Monolith

### نمای کلی معماری

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

## اصول طراحی ماژول

### 1. تعریف ماژول

هر ماژول باید:
- **خودکفا**: منطق دامنه خود را دارد
- **کم وابسته**: وابستگی‌های کم به ماژول‌های دیگر
- **بسیار منسجم**: عملکردهای مرتبط با هم گروه‌بندی شده
- **قابل تست مستقل**: می‌تواند به طور جداگانه تست شود

### 2. ساختار ماژول

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

## ارتباط ماژول

### 1. رابط ماژول

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
        // منطق مقداردهی اولیه ماژول
        await Task.CompletedTask;
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Shutting down UserModule");
        // منطق پاکسازی ماژول
        await Task.CompletedTask;
    }
}
```

### 2. سرویس ارتباط ماژول

```csharp
public interface IModuleCommunication
{
    Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default);
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
    void Subscribe<TEvent>(Func<TEvent, Task> handler);
}

public class ModuleCommunication : IModuleCommunication
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IIntegrationEventBus _eventBus;
    private readonly ILogger<ModuleCommunication> _logger;

    public ModuleCommunication(
        IServiceProvider serviceProvider,
        IIntegrationEventBus eventBus,
        ILogger<ModuleCommunication> logger)
    {
        _serviceProvider = serviceProvider;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending request {RequestType} to module", typeof(TRequest).Name);

        try
        {
            var handler = _serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>();
            if (handler == null)
            {
                throw new InvalidOperationException($"No handler found for request type {typeof(TRequest).Name}");
            }

            var response = await handler.Handle(request, cancellationToken);
            _logger.LogDebug("Request {RequestType} handled successfully", typeof(TRequest).Name);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling request {RequestType}", typeof(TRequest).Name);
            throw;
        }
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Publishing event {EventType}", typeof(TEvent).Name);
        await _eventBus.PublishAsync(@event, cancellationToken);
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> handler)
    {
        _logger.LogDebug("Subscribing to event {EventType}", typeof(TEvent).Name);
        _eventBus.Subscribe(handler);
    }
}
```

### 3. ماژول Registry

```csharp
public class ModuleRegistry
{
    private readonly Dictionary<string, IModule> _modules = new();
    private readonly ILogger<ModuleRegistry> _logger;

    public ModuleRegistry(ILogger<ModuleRegistry> logger)
    {
        _logger = logger;
    }

    public void RegisterModule(IModule module)
    {
        if (_modules.ContainsKey(module.Name))
        {
            throw new InvalidOperationException($"Module {module.Name} is already registered");
        }

        _modules[module.Name] = module;
        _logger.LogInformation("Module {ModuleName} registered", module.Name);
    }

    public IModule? GetModule(string name)
    {
        return _modules.TryGetValue(name, out var module) ? module : null;
    }

    public IEnumerable<IModule> GetAllModules()
    {
        return _modules.Values;
    }

    public async Task InitializeAllModulesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing all modules");

        foreach (var module in _modules.Values)
        {
            try
            {
                await module.InitializeAsync(cancellationToken);
                _logger.LogInformation("Module {ModuleName} initialized successfully", module.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize module {ModuleName}", module.Name);
                throw;
            }
        }
    }

    public async Task ShutdownAllModulesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Shutting down all modules");

        foreach (var module in _modules.Values.Reverse())
        {
            try
            {
                await module.ShutdownAsync(cancellationToken);
                _logger.LogInformation("Module {ModuleName} shut down successfully", module.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to shutdown module {ModuleName}", module.Name);
            }
        }
    }
}
```

## رویدادهای ادغام

### 1. Integration Event Bus

```csharp
public interface IIntegrationEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
    void Subscribe<TEvent>(Func<TEvent, Task> handler);
    void Unsubscribe<TEvent>(Func<TEvent, Task> handler);
}

public class InMemoryEventBus : IIntegrationEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly ILogger<InMemoryEventBus> _logger;

    public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
    {
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Publishing event {EventType}", typeof(TEvent).Name);

        if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            _logger.LogDebug("No handlers found for event {EventType}", typeof(TEvent).Name);
            return;
        }

        var tasks = handlers.Select(handler =>
        {
            try
            {
                var task = (Task)handler.DynamicInvoke(@event)!;
                return task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking handler for event {EventType}", typeof(TEvent).Name);
                return Task.CompletedTask;
            }
        });

        await Task.WhenAll(tasks);
        _logger.LogDebug("Event {EventType} published successfully", typeof(TEvent).Name);
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> handler)
    {
        var eventType = typeof(TEvent);
        
        if (!_handlers.ContainsKey(eventType))
        {
            _handlers[eventType] = new List<Delegate>();
        }

        _handlers[eventType].Add(handler);
        _logger.LogDebug("Subscribed to event {EventType}", eventType.Name);
    }

    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler)
    {
        var eventType = typeof(TEvent);
        
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            handlers.Remove(handler);
            _logger.LogDebug("Unsubscribed from event {EventType}", eventType.Name);
        }
    }
}
```

### 2. Integration Events

```csharp
public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredOn { get; }
    string EventType { get; }
}

public abstract class IntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public abstract string EventType { get; }
}

// User Module Events
public class UserCreatedEvent : IntegrationEvent
{
    public override string EventType => "UserCreated";
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class UserUpdatedEvent : IntegrationEvent
{
    public override string EventType => "UserUpdated";
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

// Product Module Events
public class ProductCreatedEvent : IntegrationEvent
{
    public override string EventType => "ProductCreated";
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class ProductUpdatedEvent : IntegrationEvent
{
    public override string EventType => "ProductUpdated";
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class ProductStockUpdatedEvent : IntegrationEvent
{
    public override string EventType => "ProductStockUpdated";
    public Guid ProductId { get; set; }
    public int NewStock { get; set; }
    public int PreviousStock { get; set; }
}

// Order Module Events
public class OrderCreatedEvent : IntegrationEvent
{
    public override string EventType => "OrderCreated";
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderStatusChangedEvent : IntegrationEvent
{
    public override string EventType => "OrderStatusChanged";
    public Guid OrderId { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
}
```

### 3. Event Handlers

```csharp
// User Module Event Handlers
public class UserCreatedEventHandler : IIntegrationEventHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger;

    public UserCreatedEventHandler(ILogger<UserCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(UserCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling UserCreatedEvent for user {UserId}", @event.UserId);

        // منطق پردازش رویداد
        // مثلاً ارسال ایمیل خوشامدگویی
        // یا به‌روزرسانی cache

        await Task.CompletedTask;
    }
}

// Product Module Event Handlers
public class ProductStockUpdatedEventHandler : IIntegrationEventHandler<ProductStockUpdatedEvent>
{
    private readonly ILogger<ProductStockUpdatedEventHandler> _logger;

    public ProductStockUpdatedEventHandler(ILogger<ProductStockUpdatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(ProductStockUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling ProductStockUpdatedEvent for product {ProductId}", @event.ProductId);

        // منطق پردازش رویداد
        // مثلاً بررسی کمبود موجودی
        // یا ارسال هشدار

        await Task.CompletedTask;
    }
}

// Order Module Event Handlers
public class OrderCreatedEventHandler : IIntegrationEventHandler<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedEventHandler> _logger;

    public OrderCreatedEventHandler(ILogger<OrderCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(OrderCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling OrderCreatedEvent for order {OrderId}", @event.OrderId);

        // منطق پردازش رویداد
        // مثلاً ارسال ایمیل تأیید
        // یا به‌روزرسانی موجودی محصولات

        await Task.CompletedTask;
    }
}
```

## مثال کامل: پلتفرم تجارت الکترونیک

### 1. User Module

```csharp
// Domain
public class User : AggregateRoot<Guid>
{
    public string Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public User(Guid id, string email, string firstName, string lastName) : base(id)
    {
        Email = email ?? throw new ArgumentNullException(nameof(email));
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

// Application
public class CreateUserCommand : ICommand<Guid>
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventBus _eventBus;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IIntegrationEventBus eventBus,
        ILogger<CreateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating user with email {Email}", request.Email);

        // بررسی وجود کاربر
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
            throw new InvalidOperationException("User with this email already exists");

        var user = new User(Guid.NewGuid(), request.Email, request.FirstName, request.LastName);
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // انتشار رویداد
        var userCreatedEvent = new UserCreatedEvent
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        };

        await _eventBus.PublishAsync(userCreatedEvent, cancellationToken);

        _logger.LogInformation("User {UserId} created successfully", user.Id);
        return user.Id;
    }
}

// Presentation
[ApiController]
[Route("api/users")]
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
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var userId = await _mediator.Send(command);
        return Ok(userId);
    }
}
```

### 2. Product Module

```csharp
// Domain
public class Product : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Sku { get; private set; }
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Product(Guid id, string name, string description, string sku, decimal price, int stockQuantity) : base(id)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Sku = sku ?? throw new ArgumentNullException(nameof(sku));
        Price = price;
        StockQuantity = stockQuantity;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentException("Price cannot be negative", nameof(newPrice));

        Price = newPrice;
    }

    public void UpdateStock(int newQuantity)
    {
        if (newQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative", nameof(newQuantity));

        var previousStock = StockQuantity;
        StockQuantity = newQuantity;

        // انتشار رویداد تغییر موجودی
        AddDomainEvent(new ProductStockUpdatedEvent
        {
            ProductId = Id,
            NewStock = newQuantity,
            PreviousStock = previousStock
        });
    }
}

// Application
public class UpdateProductStockCommand : ICommand
{
    public Guid ProductId { get; set; }
    public int NewStock { get; set; }
}

public class UpdateProductStockCommandHandler : ICommandHandler<UpdateProductStockCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventBus _eventBus;
    private readonly ILogger<UpdateProductStockCommandHandler> _logger;

    public UpdateProductStockCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IIntegrationEventBus eventBus,
        ILogger<UpdateProductStockCommandHandler> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(UpdateProductStockCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating stock for product {ProductId}", request.ProductId);

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
            throw new InvalidOperationException($"Product {request.ProductId} not found");

        product.UpdateStock(request.NewStock);
        await _productRepository.UpdateAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // انتشار رویداد
        var stockUpdatedEvent = new ProductStockUpdatedEvent
        {
            ProductId = product.Id,
            NewStock = product.StockQuantity,
            PreviousStock = product.StockQuantity - (request.NewStock - product.StockQuantity)
        };

        await _eventBus.PublishAsync(stockUpdatedEvent, cancellationToken);

        _logger.LogInformation("Stock updated for product {ProductId}", product.Id);
    }
}
```

### 3. Order Module

```csharp
// Domain
public class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = new();

    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public Order(Guid id, Guid customerId) : base(id)
    {
        CustomerId = customerId;
        Status = OrderStatus.Pending;
        TotalAmount = 0;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive", nameof(quantity));
        if (Status != OrderStatus.Pending) throw new InvalidOperationException("Cannot modify completed order");

        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.IncreaseQuantity(quantity);
        }
        else
        {
            var newItem = new OrderItem(productId, productName, unitPrice, quantity);
            _items.Add(newItem);
        }

        RecalculateTotal();
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending) throw new InvalidOperationException("Only pending orders can be confirmed");
        if (!_items.Any()) throw new InvalidOperationException("Cannot confirm empty order");

        Status = OrderStatus.Confirmed;
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Sum(item => item.TotalPrice);
    }
}

// Application
public class CreateOrderCommand : ICommand<Guid>
{
    public Guid CustomerId { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventBus _eventBus;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IIntegrationEventBus eventBus,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

        var order = new Order(Guid.NewGuid(), request.CustomerId);

        foreach (var item in request.Items)
        {
            order.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);
        }

        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        await _eventBus.PublishAsync(orderCreatedEvent, cancellationToken);

        _logger.LogInformation("Order {OrderId} created successfully", order.Id);
        return order.Id;
    }
}
```

## مهاجرت به Microservices

### 1. استراتژی مهاجرت

```csharp
public class MicroserviceMigrationStrategy
{
    private readonly ILogger<MicroserviceMigrationStrategy> _logger;

    public MicroserviceMigrationStrategy(ILogger<MicroserviceMigrationStrategy> logger)
    {
        _logger = logger;
    }

    public async Task<MigrationPlan> CreateMigrationPlanAsync(IModule module)
    {
        _logger.LogInformation("Creating migration plan for module {ModuleName}", module.Name);

        var plan = new MigrationPlan
        {
            ModuleName = module.Name,
            Steps = new List<MigrationStep>
            {
                new() { Name = "Extract Module", Status = MigrationStatus.Pending },
                new() { Name = "Create Service", Status = MigrationStatus.Pending },
                new() { Name = "Update Communication", Status = MigrationStatus.Pending },
                new() { Name = "Deploy Service", Status = MigrationStatus.Pending },
                new() { Name = "Remove Module", Status = MigrationStatus.Pending }
            }
        };

        return plan;
    }
}

public class MigrationPlan
{
    public string ModuleName { get; set; } = string.Empty;
    public List<MigrationStep> Steps { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class MigrationStep
{
    public string Name { get; set; } = string.Empty;
    public MigrationStatus Status { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public enum MigrationStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}
```

### 2. Service Extraction

```csharp
public class ServiceExtractor
{
    private readonly ILogger<ServiceExtractor> _logger;

    public ServiceExtractor(ILogger<ServiceExtractor> logger)
    {
        _logger = logger;
    }

    public async Task<ExtractionResult> ExtractModuleAsync(IModule module)
    {
        _logger.LogInformation("Extracting module {ModuleName} to microservice", module.Name);

        var result = new ExtractionResult
        {
            ModuleName = module.Name,
            ServiceName = $"{module.Name}Service",
            ExtractedAt = DateTimeOffset.UtcNow
        };

        // منطق استخراج ماژول
        // 1. ایجاد پروژه سرویس جدید
        // 2. کپی کردن کدهای ماژول
        // 3. تنظیم وابستگی‌ها
        // 4. ایجاد API endpoints

        return result;
    }
}

public class ExtractionResult
{
    public string ModuleName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTimeOffset ExtractedAt { get; set; }
    public List<string> Dependencies { get; set; } = new();
}
```

## بهترین شیوه‌ها

### 1. طراحی ماژول
- ماژول‌ها را بر اساس قابلیت‌های کسب‌وکار طراحی کنید
- از وابستگی‌های کم استفاده کنید
- رابط‌های واضح تعریف کنید
- از shared database با احتیاط استفاده کنید

### 2. ارتباط ماژول
- از رویدادها برای ارتباط غیرهمزمان استفاده کنید
- از direct calls برای ارتباط همزمان استفاده کنید
- error handling مناسب پیاده‌سازی کنید
- از logging برای نظارت استفاده کنید

### 3. مدیریت داده
- از database per module استفاده کنید
- از shared tables با احتیاط استفاده کنید
- migration strategies را برنامه‌ریزی کنید
- backup و restore را در نظر بگیرید

### 4. تست
- ماژول‌ها را به طور مستقل تست کنید
- integration tests را پیاده‌سازی کنید
- از test doubles استفاده کنید
- performance tests را انجام دهید

### 5. Monitoring
- metrics per module را پیگیری کنید
- health checks را پیاده‌سازی کنید
- error tracking را تنظیم کنید
- alerting مناسب ایجاد کنید

این راهنما پایه جامعی برای پیاده‌سازی modular monolith با Raziee.SharedKernel ارائه می‌دهد، شامل تمام الگوها و شیوه‌های لازم برای ساخت برنامه‌های قابل تکامل و مقیاس‌پذیر.
