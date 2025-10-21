# راهنمای Domain-Driven Design (DDD)

این راهنمای جامع نشان می‌دهد که چگونه از Raziee.SharedKernel برای پیاده‌سازی الگوهای Domain-Driven Design در برنامه‌های .NET استفاده کنید.

## فهرست مطالب

- [مقدمه](#مقدمه)
- [مفاهیم اصلی DDD](#مفاهیم-اصلی-ddd)
- [Entities و Value Objects](#entities-و-value-objects)
- [Aggregates و Domain Events](#aggregates-و-domain-events)
- [Domain Services](#domain-services)
- [الگوی Repository](#الگوی-repository)
- [نمونه کامل: سیستم E-Commerce](#نمونه-کامل-سیستم-e-commerce)
- [بهترین شیوه‌ها](#بهترین-شیوه‌ها)

## مقدمه

Domain-Driven Design (DDD) یک رویکرد توسعه نرم‌افزار است که بر مدل‌سازی دامنه‌های کسب‌وکار پیچیده تمرکز دارد. Raziee.SharedKernel بلوک‌های سازنده پایه‌ای برای پیاده‌سازی مؤثر الگوهای DDD فراهم می‌کند.

## مفاهیم اصلی DDD

### 1. Entities

Entities اشیایی با هویت متمایز هستند که در طول زمان و حالات مختلف ادامه می‌یابند.

```csharp
using Raziee.SharedKernel.Domain.Entities;

public class Customer : Entity<Guid>
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Email Email { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Customer(Guid id, string firstName, string lastName, Email email)
        : base(id)
    {
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        CreatedAt = DateTimeOffset.UtcNow;
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
}
```

### 2. Value Objects

Value objects اشیایی هستند که بر اساس ویژگی‌هایشان تعریف می‌شوند نه هویت.

```csharp
using Raziee.SharedKernel.Domain.ValueObjects;

public class Email : ValueObject
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));
        
        if (!IsValidEmail(value))
            throw new ArgumentException("Invalid email format", nameof(value));

        Value = value.ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(Email email) => email.Value;
    public static implicit operator Email(string value) => new(value);
}

public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot subtract money with different currencies");

        return new Money(Amount - other.Amount, Currency);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

### 3. Aggregate Roots

Aggregate roots entities هایی هستند که دسترسی به سایر entities درون aggregate را کنترل می‌کنند.

```csharp
using Raziee.SharedKernel.Domain.Entities;
using Raziee.SharedKernel.Domain.Events;

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

        AddDomainEvent(new OrderCreatedEvent(Id, CustomerId, CreatedAt));
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
        AddDomainEvent(new OrderItemAddedEvent(Id, product.Id, quantity));
    }

    public void RemoveItem(Guid productId)
    {
        if (Status != OrderStatus.Pending) throw new InvalidOperationException("Cannot modify completed order");

        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            _items.Remove(item);
            RecalculateTotal();
            AddDomainEvent(new OrderItemRemovedEvent(Id, productId));
        }
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending) throw new InvalidOperationException("Only pending orders can be confirmed");
        if (!_items.Any()) throw new InvalidOperationException("Cannot confirm empty order");

        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id, CustomerId, TotalAmount));
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered)
            throw new InvalidOperationException("Cannot cancel shipped or delivered orders");

        Status = OrderStatus.Cancelled;
        AddDomainEvent(new OrderCancelledEvent(Id, CustomerId));
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Aggregate(
            new Money(0, "USD"),
            (total, item) => total.Add(item.TotalPrice)
        );
    }
}

public class OrderItem : ValueObject
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public Money UnitPrice { get; }
    public int Quantity { get; private set; }
    public Money TotalPrice => new Money(UnitPrice.Amount * Quantity, UnitPrice.Currency);

    public OrderItem(Guid productId, string productName, Money unitPrice, int quantity)
    {
        ProductId = productId;
        ProductName = productName ?? throw new ArgumentNullException(nameof(productName));
        UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));
        Quantity = quantity;
    }

    public void IncreaseQuantity(int amount)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));
        Quantity += amount;
    }

    public void DecreaseQuantity(int amount)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));
        if (Quantity - amount < 0) throw new InvalidOperationException("Quantity cannot be negative");
        Quantity -= amount;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ProductId;
        yield return ProductName;
        yield return UnitPrice;
        yield return Quantity;
    }
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}
```

### 4. Domain Events

Domain events نمایانگر چیزی مهم هستند که در دامنه رخ داده است.

```csharp
using Raziee.SharedKernel.Domain.Events;

public class OrderCreatedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public DateTimeOffset CreatedAt { get; }

    public OrderCreatedEvent(Guid orderId, Guid customerId, DateTimeOffset createdAt)
    {
        OrderId = orderId;
        CustomerId = customerId;
        CreatedAt = createdAt;
    }
}

public class OrderItemAddedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid ProductId { get; }
    public int Quantity { get; }

    public OrderItemAddedEvent(Guid orderId, Guid productId, int quantity)
    {
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
    }
}

public class OrderConfirmedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public Money TotalAmount { get; }

    public OrderConfirmedEvent(Guid orderId, Guid customerId, Money totalAmount)
    {
        OrderId = orderId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
    }
}

public class OrderCancelledEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid CustomerId { get; }

    public OrderCancelledEvent(Guid orderId, Guid customerId)
    {
        OrderId = orderId;
        CustomerId = customerId;
    }
}
```

## Domain Services

Domain services شامل منطق کسب‌وکاری هستند که به طور طبیعی در entities یا value objects قرار نمی‌گیرند.

```csharp
public interface IOrderDomainService
{
    Task<bool> CanCustomerPlaceOrderAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<Money> CalculateShippingCostAsync(Order order, CancellationToken cancellationToken = default);
    Task<bool> IsProductAvailableAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
}

public class OrderDomainService : IOrderDomainService
{
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly ILogger<OrderDomainService> _logger;

    public OrderDomainService(
        IRepository<Customer> customerRepository,
        IRepository<Product> productRepository,
        ILogger<OrderDomainService> logger)
    {
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<bool> CanCustomerPlaceOrderAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null) return false;

        // قانون کسب‌وکار: مشتری باید فعال و مسدود نشده باشد
        return customer.IsActive && !customer.IsBlocked;
    }

    public async Task<Money> CalculateShippingCostAsync(Order order, CancellationToken cancellationToken = default)
    {
        // قانون کسب‌وکار: ارسال رایگان برای سفارشات بالای 100 دلار
        if (order.TotalAmount.Amount >= 100)
            return new Money(0, order.TotalAmount.Currency);

        // قانون کسب‌وکار: هزینه ارسال بر اساس وزن کل
        var totalWeight = order.Items.Sum(item => item.Quantity * 0.5m); // فرض 0.5 کیلوگرم برای هر آیتم
        var shippingCost = totalWeight * 2.5m; // 2.5 دلار برای هر کیلوگرم

        return new Money(shippingCost, order.TotalAmount.Currency);
    }

    public async Task<bool> IsProductAvailableAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product == null) return false;

        return product.IsActive && product.StockQuantity >= quantity;
    }
}
```

## الگوی Repository

Repositories انتزاعی بر روی دسترسی به داده فراهم می‌کنند.

```csharp
using Raziee.SharedKernel.Repositories;
using Raziee.SharedKernel.Specifications;

public interface IOrderRepository : IRepository<Order>
{
    Task<IEnumerable<Order>> GetOrdersByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);
    Task<Order?> GetOrderWithItemsAsync(Guid orderId, CancellationToken cancellationToken = default);
}

public class EfOrderRepository : EfRepository<Order>, IOrderRepository
{
    public EfOrderRepository(DbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Order>> GetOrdersByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var specification = new OrderByCustomerSpecification(customerId);
        return await GetAsync(specification, cancellationToken);
    }

    public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
    {
        var specification = new OrderByStatusSpecification(status);
        return await GetAsync(specification, cancellationToken);
    }

    public async Task<Order?> GetOrderWithItemsAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var specification = new OrderWithItemsSpecification(orderId);
        return await GetFirstOrDefaultAsync(specification, cancellationToken);
    }
}

// Specifications
public class OrderByCustomerSpecification : BaseSpecification<Order, Guid>
{
    public OrderByCustomerSpecification(Guid customerId)
    {
        AddCriteria(o => o.CustomerId == customerId);
        ApplyOrderBy(o => o.CreatedAt, OrderByDirection.Descending);
    }
}

public class OrderByStatusSpecification : BaseSpecification<Order, Guid>
{
    public OrderByStatusSpecification(OrderStatus status)
    {
        AddCriteria(o => o.Status == status);
        ApplyOrderBy(o => o.CreatedAt, OrderByDirection.Descending);
    }
}

public class OrderWithItemsSpecification : BaseSpecification<Order, Guid>
{
    public OrderWithItemsSpecification(Guid orderId)
    {
        AddCriteria(o => o.Id == orderId);
        AddInclude("Items"); // شامل entities مرتبط
    }
}
```

## نمونه کامل: سیستم E-Commerce

در اینجا نمونه کاملی از نحوه استفاده از تمام مفاهیم DDD با هم آورده شده:

### 1. لایه دامنه

```csharp
// Domain/Entities/Product.cs
public class Product : Entity<Guid>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Money Price { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Product(Guid id, string name, string description, Money price, int stockQuantity)
        : base(id)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Price = price ?? throw new ArgumentNullException(nameof(price));
        StockQuantity = stockQuantity;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
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
```

### 2. لایه برنامه (CQRS)

```csharp
// Application/Commands/CreateOrderCommand.cs
using Raziee.SharedKernel.CQRS;

public class CreateOrderCommand : ICommand<Guid>
{
    public Guid CustomerId { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

// Application/Commands/CreateOrderCommandHandler.cs
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IOrderDomainService _orderDomainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IOrderDomainService orderDomainService,
        IUnitOfWork unitOfWork,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _orderDomainService = orderDomainService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

        // اعتبارسنجی اینکه مشتری می‌تواند سفارش دهد
        if (!await _orderDomainService.CanCustomerPlaceOrderAsync(request.CustomerId, cancellationToken))
            throw new InvalidOperationException("Customer cannot place order");

        // ایجاد سفارش
        var order = new Order(Guid.NewGuid(), request.CustomerId);

        // اضافه کردن آیتم‌ها به سفارش
        foreach (var itemDto in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(itemDto.ProductId, cancellationToken);
            if (product == null)
                throw new InvalidOperationException($"Product {itemDto.ProductId} not found");

            if (!await _orderDomainService.IsProductAvailableAsync(itemDto.ProductId, itemDto.Quantity, cancellationToken))
                throw new InvalidOperationException($"Product {itemDto.ProductId} not available in requested quantity");

            order.AddItem(product, itemDto.Quantity);
        }

        // ذخیره سفارش
        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} created successfully", order.Id);
        return order.Id;
    }
}

// Application/Queries/GetOrderQuery.cs
public class GetOrderQuery : IQuery<OrderDto>
{
    public Guid OrderId { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

// Application/Queries/GetOrderQueryHandler.cs
public class GetOrderQueryHandler : IQueryHandler<GetOrderQuery, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<GetOrderQueryHandler> _logger;

    public GetOrderQueryHandler(IOrderRepository orderRepository, ILogger<GetOrderQueryHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<OrderDto> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetOrderWithItemsAsync(request.OrderId, cancellationToken);
        if (order == null)
            throw new InvalidOperationException($"Order {request.OrderId} not found");

        return new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Status = order.Status,
            TotalAmount = order.TotalAmount.Amount,
            Currency = order.TotalAmount.Currency,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(item => new OrderItemDto
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList()
        };
    }
}
```

### 3. لایه زیرساخت

```csharp
// Infrastructure/Data/ApplicationDbContext.cs
public class ApplicationDbContext : DbContextBase
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // پیکربندی entities
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.OwnsOne(e => e.Email, email =>
            {
                email.Property(e => e.Value).HasColumnName("Email").IsRequired();
            });
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.OwnsOne(e => e.Price, price =>
            {
                price.Property(e => e.Amount).HasColumnName("Price").HasColumnType("decimal(18,2)");
                price.Property(e => e.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.OwnsOne(e => e.TotalAmount, total =>
            {
                total.Property(e => e.Amount).HasColumnName("TotalAmount").HasColumnType("decimal(18,2)");
                total.Property(e => e.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
            entity.OwnsMany(e => e.Items, items =>
            {
                items.WithOwner().HasForeignKey("OrderId");
                items.Property(e => e.ProductId).IsRequired();
                items.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
                items.OwnsOne(e => e.UnitPrice, price =>
                {
                    price.Property(e => e.Amount).HasColumnName("UnitPrice").HasColumnType("decimal(18,2)");
                    price.Property(e => e.Currency).HasColumnName("Currency").HasMaxLength(3);
                });
                items.Property(e => e.Quantity).IsRequired();
            });
        });
    }
}
```

### 4. لایه ارائه (API Controller)

```csharp
// Controllers/OrdersController.cs
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
```

## بهترین شیوه‌ها

### 1. طراحی Entity
- entities را روی یک مسئولیت واحد متمرکز نگه دارید
- از private setters برای اعمال قوانین کسب‌وکار استفاده کنید
- ورودی‌ها را در constructors و methods اعتبارسنجی کنید
- از value objects برای مفاهیم پیچیده استفاده کنید

### 2. طراحی Value Object
- value objects را immutable کنید
- مقایسه برابری مناسب پیاده‌سازی کنید
- منطق اعتبارسنجی را شامل کنید
- از factory methods برای ایجاد پیچیده استفاده کنید

### 3. طراحی Aggregate
- aggregates را کوچک و متمرکز نگه دارید
- از domain events برای ارتباط cross-aggregate استفاده کنید
- قوانین کسب‌وکار را درون aggregates اعمال کنید
- از repositories برای persistence aggregate استفاده کنید

### 4. Domain Events
- از domain events برای side effects استفاده کنید
- events را متمرکز و خاص نگه دارید
- تمام داده‌های لازم را در events شامل کنید
- events را تا حد امکان به صورت asynchronous مدیریت کنید

### 5. الگوی Repository
- از specifications برای کوئری‌های پیچیده استفاده کنید
- repositories را روی دسترسی به داده متمرکز نگه دارید
- از unit of work برای مدیریت تراکنش استفاده کنید
- مدیریت خطای مناسب پیاده‌سازی کنید

این راهنما پایه‌ای جامع برای پیاده‌سازی DDD با Raziee.SharedKernel فراهم می‌کند. الگوها و شیوه‌های نشان داده شده در اینجا به شما کمک می‌کند تا برنامه‌های قابل نگهداری، قابل تست و مقیاس‌پذیر بسازید.
