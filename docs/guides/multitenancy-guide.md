# Multi-Tenancy Guide

This comprehensive guide demonstrates how to use Raziee.SharedKernel to implement multi-tenancy effectively in your .NET applications.

## Table of Contents

- [Introduction](#introduction)
- [Multi-Tenancy Patterns](#multi-tenancy-patterns)
- [Tenant Identification](#tenant-identification)
- [Data Isolation](#data-isolation)
- [Tenant-Aware Entities](#tenant-aware-entities)
- [Query Filtering](#query-filtering)
- [Complete Example: SaaS Application](#complete-example-saas-application)
- [Best Practices](#best-practices)

## Introduction

Multi-tenancy allows a single application instance to serve multiple tenants (customers) while keeping their data isolated. Raziee.SharedKernel provides comprehensive support for multi-tenancy with automatic query filtering and tenant-aware entities.

## Multi-Tenancy Patterns

### 1. Database per Tenant

```
┌───────────────────────────────────────────────────────────┐
│                    Database per Tenant                    │
├───────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌─────────────┐  ┌─────────────┐       │
│  │   Tenant A   │  │   Tenant B  │  │   Tenant C  │       │
│  │  Database    │  │  Database   │  │  Database   │       │
│  │              │  │             │  │             │       │
│  │ • Users      │  │ • Users     │  │ • Users     │       │
│  │ • Products   │  │ • Products  │  │ • Products  │       │
│  │ • Orders     │  │ • Orders    │  │ • Orders    │       │
│  │ • Analytics  │  │ • Analytics │  │ • Analytics │       │
│  └──────────────┘  └─────────────┘  └─────────────┘       │
│         │               │               │                 │
│         └───────────────┼───────────────┘                 │
│                         │                                 │
│  ┌─────────────────────────────────────────────────────┐  │
│  │              Application Instance                   │  │
│  │                                                     │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │  │
│  │  │   Tenant    │  │   Tenant    │  │   Tenant    │  │  │
│  │  │   Router    │  │   Context   │  │   Config    │  │  │
│  │  │             │  │             │  │             │  │  │
│  │  │ • Subdomain │  │ • Current   │  │ • Settings  │  │  │
│  │  │ • Path      │  │ • Isolation │  │ • Features  │  │  │
│  │  │ • Header    │  │ • Security  │  │ • Limits    │  │  │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  │  │
│  └─────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────┘
```

### 2. Schema per Tenant

```
┌────────────────────────────────────────────────────────────┐
│                    Schema per Tenant                       │
├────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │   Tenant A  │  │   Tenant B  │  │   Tenant C  │         │
│  │   Schema    │  │   Schema    │  │   Schema    │         │
│  │             │  │             │  │             │         │
│  │ • tenant_a  │  │ • tenant_b  │  │ • tenant_c  │         │
│  │   .users    │  │   .users    │  │   .users    │         │
│  │   .products │  │   .products │  │   .products │         │
│  │   .orders   │  │   .orders   │  │   .orders   │         │
│  └─────────────┘  └─────────────┘  └─────────────┘         │
│         │               │               │                  │
│         └───────────────┼───────────────┘                  │
│                         │                                  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │              Shared Database                         │  │
│  │                                                      │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌───────────────┐ │  │
│  │  │   Common    │  │   Tenant    │  │   Security    │ │  │
│  │  │   Tables    │  │   Isolation │  │   & Access    │ │  │
│  │  │             │  │             │  │               │ │  │
│  │  │ • tenants   │  │ • Schema    │  │ • Permissions │ │  │
│  │  │ • configs   │  │   per       │  │ • Row Level   │ │  │
│  │  │ • logs      │  │   Tenant    │  │   Security    │ │  │
│  │  └─────────────┘  └─────────────┘  └───────────────┘ │  │
│  └──────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────┘
```

### 3. Shared Database with Tenant ID

```
┌───────────────────────────────────────────────────────────┐
│                Shared Database with Tenant ID             │
├───────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────┐  │
│  │              Shared Database                        │  │
│  │                                                     │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │  │
│  │  │   Tenant A  │  │   Tenant B  │  │   Tenant C  │  │  │
│  │  │    Data     │  │    Data     │  │    Data     │  │  │
│  │  │             │  │             │  │             │  │  │
│  │  │ tenant_id=A │  │ tenant_id=B │  │ tenant_id=C │  │  │
│  │  │ • Users     │  │ • Users     │  │ • Users     │  │  │
│  │  │ • Products  │  │ • Products  │  │ • Products  │  │  │
│  │  │ • Orders    │  │ • Orders    │  │ • Orders    │  │  │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  │  │
│  └─────────────────────────────────────────────────────┘  │
│                         │                                 │
│  ┌─────────────────────────────────────────────────────┐  │
│  │              Application Instance                   │  │
│  │                                                     │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌──────────────┐ │  │
│  │  │   Tenant    │  │   Query     │  │   Security   │ │  │
│  │  │   Filter    │  │   Filter    │  │   & Access   │ │  │
│  │  │             │  │             │  │              │ │  │
│  │  │ • Context   │  │ • WHERE     │  │ • Row Level  │ │  │
│  │  │ • Router    │  │   tenant_id │  │   Security   │ │  │
│  │  │ • Provider  │  │ • Global    │  │ • Permissions│ │  │
│  │  └─────────────┘  └─────────────┘  └──────────────┘ │  │
│  └─────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────┘
```

## Tenant Identification

### 1. Tenant Provider Interface

```csharp
using Raziee.SharedKernel.MultiTenancy;

public interface ITenantProvider
{
    string? GetCurrentTenant();
    void SetCurrentTenant(string tenantId);
    bool HasTenant();
}

public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantProvider> _logger;

    public TenantProvider(IHttpContextAccessor httpContextAccessor, ILogger<TenantProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string? GetCurrentTenant()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        // Try to get tenant from various sources
        var tenantId = GetTenantFromHeader(httpContext) ??
                      GetTenantFromQuery(httpContext) ??
                      GetTenantFromRoute(httpContext) ??
                      GetTenantFromClaim(httpContext);

        if (tenantId != null)
        {
            _logger.LogDebug("Current tenant: {TenantId}", tenantId);
        }

        return tenantId;
    }

    public void SetCurrentTenant(string tenantId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        httpContext.Items["TenantId"] = tenantId;
        _logger.LogDebug("Set current tenant: {TenantId}", tenantId);
    }

    public bool HasTenant()
    {
        return GetCurrentTenant() != null;
    }

    private string? GetTenantFromHeader(HttpContext httpContext)
    {
        return httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
    }

    private string? GetTenantFromQuery(HttpContext httpContext)
    {
        return httpContext.Request.Query["tenantId"].FirstOrDefault();
    }

    private string? GetTenantFromRoute(HttpContext httpContext)
    {
        return httpContext.Request.RouteValues["tenantId"]?.ToString();
    }

    private string? GetTenantFromClaim(HttpContext httpContext)
    {
        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated != true) return null;

        return user.FindFirst("tenant_id")?.Value;
    }
}
```

### 2. Tenant Middleware

```csharp
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
    {
        var tenantId = tenantProvider.GetCurrentTenant();
        
        if (tenantId != null)
        {
            _logger.LogDebug("Processing request for tenant {TenantId}", tenantId);
            
            // Set tenant in context for downstream components
            context.Items["TenantId"] = tenantId;
        }
        else
        {
            _logger.LogWarning("No tenant identified for request");
        }

        await _next(context);
    }
}

// Extension method for easy registration
public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}
```

### 3. Tenant Resolution Strategies

```csharp
public class SubdomainTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SubdomainTenantProvider> _logger;

    public SubdomainTenantProvider(IHttpContextAccessor httpContextAccessor, ILogger<SubdomainTenantProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string? GetCurrentTenant()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        var host = httpContext.Request.Host.Host;
        var parts = host.Split('.');
        
        if (parts.Length >= 3) // tenant.domain.com
        {
            var tenantId = parts[0];
            _logger.LogDebug("Resolved tenant from subdomain: {TenantId}", tenantId);
            return tenantId;
        }

        return null;
    }

    public void SetCurrentTenant(string tenantId)
    {
        // Not applicable for subdomain-based resolution
    }

    public bool HasTenant()
    {
        return GetCurrentTenant() != null;
    }
}

public class PathTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PathTenantProvider> _logger;

    public PathTenantProvider(IHttpContextAccessor httpContextAccessor, ILogger<PathTenantProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string? GetCurrentTenant()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        var path = httpContext.Request.Path.Value;
        var parts = path?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts?.Length >= 2 && parts[0] == "tenant")
        {
            var tenantId = parts[1];
            _logger.LogDebug("Resolved tenant from path: {TenantId}", tenantId);
            return tenantId;
        }

        return null;
    }

    public void SetCurrentTenant(string tenantId)
    {
        // Not applicable for path-based resolution
    }

    public bool HasTenant()
    {
        return GetCurrentTenant() != null;
    }
}
```

## Data Isolation

### 1. Tenant-Aware Entities

```csharp
using Raziee.SharedKernel.MultiTenancy;

public interface ITenantEntity
{
    string TenantId { get; }
}

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

### 2. Tenant Query Filter

```csharp
using Raziee.SharedKernel.MultiTenancy;

public class TenantQueryFilter<TEntity> : IQueryFilter<TEntity> where TEntity : class, ITenantEntity
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TenantQueryFilter<TEntity>> _logger;

    public TenantQueryFilter(ITenantProvider tenantProvider, ILogger<TenantQueryFilter<TEntity>> logger)
    {
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query)
    {
        var tenantId = _tenantProvider.GetCurrentTenant();
        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("No tenant context available, returning empty query");
            return query.Where(e => false); // Return empty result
        }

        _logger.LogDebug("Applying tenant filter for tenant {TenantId}", tenantId);
        return query.Where(e => e.TenantId == tenantId);
    }
}

public interface IQueryFilter<TEntity>
{
    IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query);
}
```

### 3. Tenant-Aware Repository

```csharp
public class TenantAwareRepository<TEntity> : EfRepository<TEntity> 
    where TEntity : class, ITenantEntity
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TenantAwareRepository<TEntity>> _logger;

    public TenantAwareRepository(
        DbContext context, 
        ITenantProvider tenantProvider,
        ILogger<TenantAwareRepository<TEntity>> logger) : base(context)
    {
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public override async Task<TEntity?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default)
    {
        var entity = await base.GetByIdAsync(id, cancellationToken);
        if (entity == null) return null;

        var tenantId = _tenantProvider.GetCurrentTenant();
        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("No tenant context available");
            return null;
        }

        if (entity.TenantId != tenantId)
        {
            _logger.LogWarning("Entity {EntityId} belongs to different tenant {EntityTenantId}, expected {CurrentTenantId}", 
                entity.Id, entity.TenantId, tenantId);
            return null;
        }

        return entity;
    }

    public override async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenant();
        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("No tenant context available");
            return new List<TEntity>();
        }

        return await _dbSet.Where(e => e.TenantId == tenantId).ToListAsync(cancellationToken);
    }

    public override async Task<IEnumerable<TEntity>> GetAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenant();
        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("No tenant context available");
            return new List<TEntity>();
        }

        var query = ApplySpecification(specification);
        return await query.Where(e => e.TenantId == tenantId).ToListAsync(cancellationToken);
    }

    public override async Task<TEntity?> GetFirstOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenant();
        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("No tenant context available");
            return null;
        }

        var query = ApplySpecification(specification);
        return await query.Where(e => e.TenantId == tenantId).FirstOrDefaultAsync(cancellationToken);
    }

    public override async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenant();
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new InvalidOperationException("No tenant context available");
        }

        // Ensure entity has correct tenant ID
        entity.GetType().GetProperty("TenantId")?.SetValue(entity, tenantId);
        await base.AddAsync(entity, cancellationToken);
    }
}
```

## Query Filtering

### 1. Automatic Query Filtering

```csharp
public class TenantQueryFilterService
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TenantQueryFilterService> _logger;

    public TenantQueryFilterService(ITenantProvider tenantProvider, ILogger<TenantQueryFilterService> logger)
    {
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public IQueryable<TEntity> ApplyTenantFilter<TEntity>(IQueryable<TEntity> query) where TEntity : class, ITenantEntity
    {
        var tenantId = _tenantProvider.GetCurrentTenant();
        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("No tenant context available, returning empty query");
            return query.Where(e => false);
        }

        _logger.LogDebug("Applying tenant filter for tenant {TenantId}", tenantId);
        return query.Where(e => e.TenantId == tenantId);
    }
}
```

### 2. Specification with Tenant Filtering

```csharp
public class TenantAwareSpecification<TEntity> : BaseSpecification<TEntity, Guid> where TEntity : class, ITenantEntity
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TenantAwareSpecification<TEntity>> _logger;

    public TenantAwareSpecification(ITenantProvider tenantProvider, ILogger<TenantAwareSpecification<TEntity>> logger)
    {
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    protected override IQueryable<TEntity> ApplySpecification(IQueryable<TEntity> query)
    {
        var tenantId = _tenantProvider.GetCurrentTenant();
        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("No tenant context available");
            return query.Where(e => false);
        }

        _logger.LogDebug("Applying tenant filter for tenant {TenantId}", tenantId);
        return base.ApplySpecification(query.Where(e => e.TenantId == tenantId));
    }
}

public class CustomerByEmailSpecification : TenantAwareSpecification<Customer>
{
    public CustomerByEmailSpecification(string email, ITenantProvider tenantProvider, ILogger<TenantAwareSpecification<Customer>> logger) 
        : base(tenantProvider, logger)
    {
        AddCriteria(c => c.Email.Value == email);
    }
}

public class ActiveCustomersSpecification : TenantAwareSpecification<Customer>
{
    public ActiveCustomersSpecification(ITenantProvider tenantProvider, ILogger<TenantAwareSpecification<Customer>> logger) 
        : base(tenantProvider, logger)
    {
        AddCriteria(c => c.IsActive);
        ApplyOrderBy(c => c.CreatedAt, OrderByDirection.Descending);
    }
}
```

## Complete Example: SaaS Application

### 1. Domain Entities

```csharp
public class Tenant : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Subdomain { get; private set; }
    public string DatabaseConnectionString { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Tenant(Guid id, string name, string subdomain, string databaseConnectionString) : base(id)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Subdomain = subdomain ?? throw new ArgumentNullException(nameof(subdomain));
        DatabaseConnectionString = databaseConnectionString ?? throw new ArgumentNullException(nameof(databaseConnectionString));
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateName(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

public class User : AggregateRoot<Guid>, ITenantEntity
{
    public string TenantId { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Email Email { get; private set; }
    public string Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public User(Guid id, string tenantId, string firstName, string lastName, Email email, string role) : base(id)
    {
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Role = role ?? throw new ArgumentNullException(nameof(role));
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateName(string firstName, string lastName)
    {
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
    }

    public void UpdateRole(string role)
    {
        Role = role ?? throw new ArgumentNullException(nameof(role));
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
```

### 2. Application Services

```csharp
public class TenantService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        ILogger<TenantService> logger)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating tenant {TenantName} with subdomain {Subdomain}", request.Name, request.Subdomain);

        // Check if subdomain is already taken
        var existingTenant = await _tenantRepository.GetBySubdomainAsync(request.Subdomain, cancellationToken);
        if (existingTenant != null)
            throw new InvalidOperationException($"Subdomain {request.Subdomain} is already taken");

        var tenant = new Tenant(Guid.NewGuid(), request.Name, request.Subdomain, request.DatabaseConnectionString);
        await _tenantRepository.AddAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tenant {TenantId} created successfully", tenant.Id);
        return tenant.Id;
    }

    public async Task<TenantDto> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
            throw new InvalidOperationException($"Tenant {tenantId} not found");

        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt
        };
    }
}

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenant();
        if (string.IsNullOrEmpty(tenantId))
            throw new InvalidOperationException("No tenant context available");

        _logger.LogInformation("Creating user {Email} for tenant {TenantId}", request.Email, tenantId);

        // Check if user already exists in this tenant
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
            throw new InvalidOperationException("User with this email already exists");

        var user = new User(Guid.NewGuid(), tenantId, request.FirstName, request.LastName, new Email(request.Email), request.Role);
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} created successfully for tenant {TenantId}", user.Id, tenantId);
        return user.Id;
    }

    public async Task<UserDto> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new InvalidOperationException($"User {userId} not found");

        return new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email.Value,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<IEnumerable<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.Select(u => new UserDto
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email.Value,
            Role = u.Role,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        });
    }
}
```

### 3. API Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly TenantService _tenantService;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(TenantService tenantService, ILogger<TenantsController> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateTenant(CreateTenantRequest request)
    {
        var tenantId = await _tenantService.CreateTenantAsync(request);
        return Ok(tenantId);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TenantDto>> GetTenant(Guid id)
    {
        var tenant = await _tenantService.GetTenantAsync(id);
        return Ok(tenant);
    }
}

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateUser(CreateUserRequest request)
    {
        var userId = await _userService.CreateUserAsync(request);
        return Ok(userId);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        var user = await _userService.GetUserAsync(id);
        return Ok(user);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _userService.GetUsersAsync();
        return Ok(users);
    }
}
```

### 4. Database Configuration

```csharp
public class ApplicationDbContext : DbContextBase
{
    private readonly ITenantProvider _tenantProvider;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantProvider tenantProvider) : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure tenant entities
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Subdomain).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Subdomain).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.OwnsOne(e => e.Email, email =>
            {
                email.Property(e => e.Value).HasColumnName("Email").IsRequired();
            });
            entity.HasIndex(e => new { e.TenantId, e.Email });
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.OwnsOne(e => e.Email, email =>
            {
                email.Property(e => e.Value).HasColumnName("Email").IsRequired();
            });
            entity.HasIndex(e => new { e.TenantId, e.Email });
        });

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
        });

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

### 5. Service Registration

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add Raziee.SharedKernel
        builder.Services.AddSharedKernel();

        // Add multi-tenancy services
        builder.Services.AddScoped<ITenantProvider, TenantProvider>();
        builder.Services.AddScoped<ITenantQueryFilterService, TenantQueryFilterService>();

        // Add repositories
        builder.Services.AddScoped<ITenantRepository, EfTenantRepository>();
        builder.Services.AddScoped<IUserRepository, EfUserRepository>();
        builder.Services.AddScoped<ICustomerRepository, EfCustomerRepository>();
        builder.Services.AddScoped<IProductRepository, EfProductRepository>();
        builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();

        // Add services
        builder.Services.AddScoped<TenantService>();
        builder.Services.AddScoped<UserService>();
        builder.Services.AddScoped<CustomerService>();
        builder.Services.AddScoped<ProductService>();
        builder.Services.AddScoped<OrderService>();

        // Add Entity Framework
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        var app = builder.Build();

        // Add middleware
        app.UseTenantMiddleware();

        app.Run();
    }
}
```

## Best Practices

### 1. Tenant Identification
- Use consistent tenant identification across all requests
- Implement multiple tenant resolution strategies
- Handle tenant context properly in background services
- Use secure tenant identification methods

### 2. Data Isolation
- Implement proper tenant filtering at the repository level
- Use tenant-aware entities consistently
- Validate tenant context in business logic
- Implement proper error handling for missing tenant context

### 3. Performance Considerations
- Use appropriate database indexes for tenant queries
- Implement proper caching strategies
- Consider database partitioning for large datasets
- Monitor query performance across tenants

### 4. Security
- Implement proper tenant isolation
- Use secure tenant identification
- Implement proper access control
- Audit tenant-specific operations

### 5. Testing
- Test tenant isolation thoroughly
- Use different tenant contexts in tests
- Test tenant switching scenarios
- Implement proper test data setup

This guide provides a comprehensive foundation for implementing multi-tenancy with Raziee.SharedKernel, including all the necessary patterns and practices for building secure and scalable multi-tenant applications.
