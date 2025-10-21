# Entity Framework Extensions Guide

This comprehensive guide demonstrates how to use Raziee.SharedKernel Entity Framework extensions for database configuration, audit fields, and domain events in your .NET applications.

## Table of Contents

- [Introduction](#introduction)
- [ModelBuilder Extensions](#modelbuilder-extensions)
- [Auditable Entity Interceptor](#auditable-entity-interceptor)
- [DbContext Base](#dbcontext-base)
- [Complete Example: E-Commerce Database](#complete-example-e-commerce-database)
- [Best Practices](#best-practices)

## Introduction

Entity Framework extensions provide common database configuration patterns and automatic handling of audit fields and domain events. Raziee.SharedKernel includes comprehensive extensions for Entity Framework Core.

## ModelBuilder Extensions

### 1. Auditable Entities Configuration

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

        // Configure auditable entities
        modelBuilder.ConfigureAuditableEntities();

        // Configure soft delete
        modelBuilder.ConfigureSoftDelete();

        // Configure concurrency tokens
        modelBuilder.ConfigureConcurrencyTokens();

        // Configure decimal precision
        modelBuilder.ConfigureDecimalPrecision(18, 2);

        // Configure string max length
        modelBuilder.ConfigureStringMaxLength(256);

        // Configure specific entities
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

### 2. Soft Delete Configuration

```csharp
public class SoftDeleteEntity : Entity<Guid>, ISoftDelete
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

// Usage in DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Configure soft delete for entities implementing ISoftDelete
    modelBuilder.ConfigureSoftDelete();
    
    // Add global query filter for soft delete
    modelBuilder.Entity<SoftDeleteEntity>()
        .HasQueryFilter(e => !e.IsDeleted);
}
```

### 3. Concurrency Tokens Configuration

```csharp
public class ConcurrencyEntity : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

// Usage in DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Configure concurrency tokens for all entities
    modelBuilder.ConfigureConcurrencyTokens();
    
    // Or configure for specific entity
    modelBuilder.Entity<ConcurrencyEntity>()
        .Property(e => e.RowVersion)
        .IsRowVersion();
}
```

### 4. Decimal Precision Configuration

```csharp
public class MoneyEntity : Entity<Guid>
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}

// Usage in DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Configure decimal precision for all decimal properties
    modelBuilder.ConfigureDecimalPrecision(18, 2);
    
    // Or configure for specific property
    modelBuilder.Entity<MoneyEntity>()
        .Property(e => e.Amount)
        .HasColumnType("decimal(18,2)");
}
```

### 5. String Max Length Configuration

```csharp
public class StringEntity : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

// Usage in DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Configure string max length for all string properties
    modelBuilder.ConfigureStringMaxLength(256);
    
    // Or configure for specific properties
    modelBuilder.Entity<StringEntity>(entity =>
    {
        entity.Property(e => e.Name).HasMaxLength(100);
        entity.Property(e => e.Description).HasMaxLength(500);
    });
}
```

## Auditable Entity Interceptor

### 1. Current User Service

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

### 2. Auditable Entity Interceptor Usage

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
        
        // Add auditable entity interceptor
        optionsBuilder.AddInterceptors(new AuditableEntityInterceptor(
            new CurrentUserService(new HttpContextAccessor())));
    }
}
```

### 3. Custom Auditable Entity

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

        // Configure entities
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

### 2. Custom DbContext with Additional Features

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

        // Configure multi-tenancy
        ConfigureMultiTenancy(modelBuilder);

        // Configure audit fields
        modelBuilder.ConfigureAuditableEntities();

        // Configure soft delete
        modelBuilder.ConfigureSoftDelete();

        // Configure entities
        ConfigureEntities(modelBuilder);
    }

    private void ConfigureMultiTenancy(ModelBuilder modelBuilder)
    {
        // Configure tenant-aware entities
        var tenantEntities = modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(ITenantEntity).IsAssignableFrom(e.ClrType))
            .ToList();

        foreach (var entityType in tenantEntities)
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property("TenantId")
                .IsRequired()
                .HasMaxLength(50);

            // Add global query filter for tenant
            var entityTypeBuilder = modelBuilder.Entity(entityType.ClrType);
            entityTypeBuilder.HasQueryFilter(e => EF.Property<string>(e, "TenantId") == _tenantProvider.GetCurrentTenant());
        }
    }

    private void ConfigureEntities(ModelBuilder modelBuilder)
    {
        // Configure Customer entity
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
        });

        // Configure Product entity
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

        // Configure Order entity
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

## Complete Example: E-Commerce Database

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

        // Configure common settings
        modelBuilder.ConfigureAuditableEntities();
        modelBuilder.ConfigureSoftDelete();
        modelBuilder.ConfigureConcurrencyTokens();
        modelBuilder.ConfigureDecimalPrecision(18, 2);
        modelBuilder.ConfigureStringMaxLength(256);

        // Configure multi-tenancy
        ConfigureMultiTenancy(modelBuilder);

        // Configure entities
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

            // Add global query filter for tenant
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

### 3. Service Registration

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add Raziee.SharedKernel
        builder.Services.AddSharedKernel();

        // Add Entity Framework
        builder.Services.AddDbContext<ECommerceDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            options.AddInterceptors(new AuditableEntityInterceptor(
                new CurrentUserService(new HttpContextAccessor())));
        });

        // Add services
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

## Best Practices

### 1. Model Configuration
- Use extension methods for common configurations
- Configure entities in separate methods
- Use meaningful property names
- Set appropriate constraints

### 2. Audit Fields
- Implement proper user context
- Use interceptors for automatic audit
- Handle anonymous users appropriately
- Log audit field changes

### 3. Multi-Tenancy
- Use global query filters
- Implement proper tenant isolation
- Handle tenant context properly
- Test tenant switching scenarios

### 4. Performance
- Use appropriate indexes
- Configure decimal precision
- Set string max lengths
- Use concurrency tokens

### 5. Testing
- Test database configurations
- Test audit field behavior
- Test multi-tenancy isolation
- Test domain event dispatching

This guide provides a comprehensive foundation for using Entity Framework extensions with Raziee.SharedKernel, including all the necessary patterns and practices for building robust and maintainable database layers.
