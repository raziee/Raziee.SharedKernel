# راهنمای Entity Framework Extensions

این راهنمای جامع نحوه استفاده از Entity Framework extensions در Raziee.SharedKernel برای پیکربندی دیتابیس، فیلدهای audit، و domain events در برنامه‌های .NET شما را نشان می‌دهد.

## فهرست مطالب

- [مقدمه](#مقدمه)
- [ModelBuilder Extensions](#modelbuilder-extensions)
- [Auditable Entity Interceptor](#auditable-entity-interceptor)
- [DbContext Base](#dbcontext-base)
- [مثال کامل: دیتابیس تجارت الکترونیک](#مثال-کامل-دیتابیس-تجارت-الکترونیک)
- [بهترین شیوه‌ها](#بهترین-شیوه‌ها)

## مقدمه

Entity Framework extensions الگوهای پیکربندی دیتابیس مشترک و مدیریت خودکار فیلدهای audit و domain events فراهم می‌کنند. Raziee.SharedKernel شامل extensions جامع برای Entity Framework Core است.

## ModelBuilder Extensions

### 1. پیکربندی Auditable Entities

```csharp
using Raziee.SharedKernel.Data;
using Microsoft.EntityFrameworkCore;

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

        // پیکربندی auditable entities
        modelBuilder.ConfigureAuditableEntities();

        // پیکربندی soft delete
        modelBuilder.ConfigureSoftDelete();

        // پیکربندی concurrency tokens
        modelBuilder.ConfigureConcurrencyTokens();

        // پیکربندی decimal precision
        modelBuilder.ConfigureDecimalPrecision(18, 2);

        // پیکربندی string max length
        modelBuilder.ConfigureStringMaxLength(256);

        // پیکربندی entities خاص
        ConfigureCustomerEntity(modelBuilder);
        ConfigureProductEntity(modelBuilder);
        ConfigureOrderEntity(modelBuilder);
    }

    private void ConfigureCustomerEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }

    private void ConfigureProductEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.StockQuantity).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
        });
    }

    private void ConfigureOrderEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            
            entity.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
```

### 2. پیکربندی Soft Delete

```csharp
public class SoftDeleteEntity : Entity<Guid>, ISoftDelete
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

// استفاده در DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // پیکربندی soft delete برای entities که ISoftDelete را پیاده‌سازی می‌کنند
    modelBuilder.ConfigureSoftDelete();
    
    // افزودن global query filter برای soft delete
    modelBuilder.Entity<SoftDeleteEntity>()
        .HasQueryFilter(e => !e.IsDeleted);
}
```

### 3. پیکربندی Concurrency Tokens

```csharp
public class ConcurrencyEntity : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

// استفاده در DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // پیکربندی concurrency tokens برای تمام entities
    modelBuilder.ConfigureConcurrencyTokens();
    
    // یا پیکربندی برای entity خاص
    modelBuilder.Entity<ConcurrencyEntity>()
        .Property(e => e.RowVersion)
        .IsRowVersion();
}
```

### 4. پیکربندی Decimal Precision

```csharp
public class MoneyEntity : Entity<Guid>
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}

// استفاده در DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // پیکربندی decimal precision برای تمام properties decimal
    modelBuilder.ConfigureDecimalPrecision(18, 2);
    
    // یا پیکربندی برای property خاص
    modelBuilder.Entity<MoneyEntity>()
        .Property(e => e.Amount)
        .HasColumnType("decimal(18,2)");
}
```

### 5. پیکربندی String Max Length

```csharp
public class StringEntity : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

// استفاده در DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // پیکربندی string max length برای تمام properties string
    modelBuilder.ConfigureStringMaxLength(256);
    
    // یا پیکربندی برای properties خاص
    modelBuilder.Entity<StringEntity>(entity =>
    {
        entity.Property(e => e.Name).HasMaxLength(100);
        entity.Property(e => e.Description).HasMaxLength(500);
    });
}
```

## Auditable Entity Interceptor

### 1. سرویس کاربر فعلی

```csharp
using Raziee.SharedKernel.Data;

public interface ICurrentUserService
{
    string? GetCurrentUser();
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentUser()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return null;

        return httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               httpContext.User.FindFirst("sub")?.Value ??
               httpContext.User.Identity.Name;
    }
}
```

### 2. استفاده از Auditable Entity Interceptor

```csharp
public class ApplicationDbContext : DbContextBase
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IDomainEventDispatcher domainEventDispatcher,
        ILogger<ApplicationDbContext> logger) 
        : base(options, domainEventDispatcher, logger)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        // افزودن auditable entity interceptor
        optionsBuilder.AddInterceptors(new AuditableEntityInterceptor(
            new CurrentUserService(new HttpContextAccessor())));
    }
}
```

### 3. Auditable Entity سفارشی

```csharp
public class AuditableCustomer : AuditableEntity<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public AuditableCustomer(Guid id, string firstName, string lastName, string email) : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
    }

    public void UpdateName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
```

## DbContext Base

### 1. Domain Event Dispatching

```csharp
public class ApplicationDbContext : DbContextBase
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IDomainEventDispatcher domainEventDispatcher,
        ILogger<ApplicationDbContext> logger) 
        : base(options, domainEventDispatcher, logger)
    {
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // پیکربندی entities
        ConfigureCustomerEntity(modelBuilder);
        ConfigureProductEntity(modelBuilder);
        ConfigureOrderEntity(modelBuilder);
    }

    private void ConfigureCustomerEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }

    private void ConfigureProductEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.StockQuantity).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
        });
    }

    private void ConfigureOrderEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            
            entity.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
```

### 2. DbContext سفارشی با ویژگی‌های اضافی

```csharp
public class AdvancedApplicationDbContext : DbContextBase
{
    private readonly ITenantProvider _tenantProvider;

    public AdvancedApplicationDbContext(
        DbContextOptions<AdvancedApplicationDbContext> options,
        IDomainEventDispatcher domainEventDispatcher,
        ITenantProvider tenantProvider,
        ILogger<AdvancedApplicationDbContext> logger) 
        : base(options, domainEventDispatcher, logger)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // پیکربندی multi-tenancy
        ConfigureMultiTenancy(modelBuilder);

        // پیکربندی audit fields
        modelBuilder.ConfigureAuditableEntities();

        // پیکربندی soft delete
        modelBuilder.ConfigureSoftDelete();

        // پیکربندی entities
        ConfigureEntities(modelBuilder);
    }

    private void ConfigureMultiTenancy(ModelBuilder modelBuilder)
    {
        // پیکربندی tenant-aware entities
        var tenantEntities = modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(ITenantEntity).IsAssignableFrom(e.ClrType))
            .ToList();

        foreach (var entityType in tenantEntities)
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property("TenantId")
                .IsRequired()
                .HasMaxLength(50);

            // افزودن global query filter برای tenant
            var entityTypeBuilder = modelBuilder.Entity(entityType.ClrType);
            entityTypeBuilder.HasQueryFilter(e => EF.Property<string>(e, "TenantId") == _tenantProvider.GetCurrentTenant());
        }
    }

    private void ConfigureEntities(ModelBuilder modelBuilder)
    {
        // پیکربندی Customer entity
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
        });

        // پیکربندی Product entity
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.StockQuantity).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
        });

        // پیکربندی Order entity
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CustomerId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            
            entity.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
```

## مثال کامل: دیتابیس تجارت الکترونیک

### 1. Domain Entities

```csharp
public class Customer : AggregateRoot<Guid>, ITenantEntity
{
    public string TenantId { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Email Email { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Customer(Guid id, string tenantId, string firstName, string lastName, Email email) : base(id)
    {
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateName(string firstName, string lastName)
    {
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

public class Product : AggregateRoot<Guid>, ITenantEntity
{
    public string TenantId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Money Price { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Product(Guid id, string tenantId, string name, string description, Money price, int stockQuantity) : base(id)
    {
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
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

public class Order : AggregateRoot<Guid>, ITenantEntity
{
    private readonly List<OrderItem> _items = new();

    public string TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public Order(Guid id, string tenantId, Guid customerId) : base(id)
    {
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        CustomerId = customerId;
        Status = OrderStatus.Pending;
        TotalAmount = new Money(0, "USD");
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void AddItem(Product product, int quantity)
    {
        if (product == null) throw new ArgumentNullException(nameof(product));
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive", nameof(quantity));
        if (Status != OrderStatus.Pending) throw new InvalidOperationException("Cannot modify completed order");
        if (product.TenantId != TenantId) throw new InvalidOperationException("Product belongs to different tenant");

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
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Aggregate(
            new Money(0, "USD"),
            (total, item) => total.Add(item.TotalPrice)
        );
    }
}
```

### 2. Database Context

```csharp
public class ECommerceDbContext : DbContextBase
{
    private readonly ITenantProvider _tenantProvider;

    public ECommerceDbContext(
        DbContextOptions<ECommerceDbContext> options,
        IDomainEventDispatcher domainEventDispatcher,
        ITenantProvider tenantProvider,
        ILogger<ECommerceDbContext> logger) 
        : base(options, domainEventDispatcher, logger)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // پیکربندی تنظیمات مشترک
        modelBuilder.ConfigureAuditableEntities();
        modelBuilder.ConfigureSoftDelete();
        modelBuilder.ConfigureConcurrencyTokens();
        modelBuilder.ConfigureDecimalPrecision(18, 2);
        modelBuilder.ConfigureStringMaxLength(256);

        // پیکربندی multi-tenancy
        ConfigureMultiTenancy(modelBuilder);

        // پیکربندی entities
        ConfigureCustomerEntity(modelBuilder);
        ConfigureProductEntity(modelBuilder);
        ConfigureOrderEntity(modelBuilder);
    }

    private void ConfigureMultiTenancy(ModelBuilder modelBuilder)
    {
        var tenantEntities = modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(ITenantEntity).IsAssignableFrom(e.ClrType))
            .ToList();

        foreach (var entityType in tenantEntities)
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property("TenantId")
                .IsRequired()
                .HasMaxLength(50);

            // افزودن global query filter برای tenant
            var entityTypeBuilder = modelBuilder.Entity(entityType.ClrType);
            entityTypeBuilder.HasQueryFilter(e => EF.Property<string>(e, "TenantId") == _tenantProvider.GetCurrentTenant());
        }
    }

    private void ConfigureCustomerEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.OwnsOne(e => e.Email, email =>
            {
                email.Property(e => e.Value).HasColumnName("Email").IsRequired().HasMaxLength(255);
            });
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasIndex(e => new { e.TenantId, e.Email.Value }).IsUnique();
        });
    }

    private void ConfigureProductEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.OwnsOne(e => e.Price, price =>
            {
                price.Property(e => e.Amount).HasColumnName("Price").HasColumnType("decimal(18,2)");
                price.Property(e => e.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
            entity.Property(e => e.StockQuantity).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
        });
    }

    private void ConfigureOrderEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CustomerId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.OwnsOne(e => e.TotalAmount, total =>
            {
                total.Property(e => e.Amount).HasColumnName("TotalAmount").HasColumnType("decimal(18,2)");
                total.Property(e => e.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
            entity.Property(e => e.CreatedAt).IsRequired();
            
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
            
            entity.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
```

### 3. ثبت سرویس‌ها

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // افزودن Raziee.SharedKernel
        builder.Services.AddSharedKernel();

        // افزودن Entity Framework
        builder.Services.AddDbContext<ECommerceDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            options.AddInterceptors(new AuditableEntityInterceptor(
                new CurrentUserService(new HttpContextAccessor())));
        });

        // افزودن سرویس‌ها
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddScoped<ITenantProvider, TenantProvider>();
        builder.Services.AddScoped<ICustomerRepository, EfCustomerRepository>();
        builder.Services.AddScoped<IProductRepository, EfProductRepository>();
        builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();

        var app = builder.Build();

        app.Run();
    }
}
```

## بهترین شیوه‌ها

### 1. پیکربندی Model
- از extension methods برای پیکربندی‌های مشترک استفاده کنید
- entities را در متدهای جداگانه پیکربندی کنید
- از نام‌های معنادار برای properties استفاده کنید
- محدودیت‌های مناسب تنظیم کنید

### 2. فیلدهای Audit
- context کاربر مناسب پیاده‌سازی کنید
- از interceptors برای audit خودکار استفاده کنید
- کاربران ناشناس را به‌طور مناسب مدیریت کنید
- تغییرات فیلدهای audit را لاگ کنید

### 3. Multi-Tenancy
- از global query filters استفاده کنید
- جداسازی tenant مناسب پیاده‌سازی کنید
- context tenant را به‌طور مناسب مدیریت کنید
- سناریوهای تغییر tenant را تست کنید

### 4. عملکرد
- از indexes مناسب استفاده کنید
- decimal precision را پیکربندی کنید
- string max lengths را تنظیم کنید
- از concurrency tokens استفاده کنید

### 5. تست
- پیکربندی‌های دیتابیس را تست کنید
- رفتار فیلدهای audit را تست کنید
- جداسازی multi-tenancy را تست کنید
- domain event dispatching را تست کنید

این راهنما پایه جامعی برای استفاده از Entity Framework extensions با Raziee.SharedKernel ارائه می‌دهد، شامل تمام الگوها و شیوه‌های لازم برای ساخت لایه‌های دیتابیس قوی و قابل نگهداری.
