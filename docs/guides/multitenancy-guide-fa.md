# راهنمای Multi-Tenancy

این راهنمای جامع نحوه استفاده از Raziee.SharedKernel برای پیاده‌سازی multi-tenancy به طور مؤثر در برنامه‌های .NET شما را نشان می‌دهد.

## فهرست مطالب

- [مقدمه](#مقدمه)
- [الگوهای Multi-Tenancy](#الگوهای-multi-tenancy)
- [شناسایی Tenant](#شناسایی-tenant)
- [جداسازی داده](#جداسازی-داده)
- [موجودیت‌های آگاه از Tenant](#موجودیت‌های-آگاه-از-tenant)
- [فیلتر کردن پرس‌وجو](#فیلتر-کردن-پرس‌وجو)
- [مثال کامل: برنامه SaaS](#مثال-کامل-برنامه-saas)
- [بهترین شیوه‌ها](#بهترین-شیوه‌ها)

## مقدمه

Multi-tenancy امکان سرویس‌دهی به چندین tenant (مشتری) توسط یک نمونه برنامه واحد را فراهم می‌کند در حالی که داده‌های آن‌ها جدا نگه داشته می‌شود. Raziee.SharedKernel پشتیبانی جامعی از multi-tenancy با فیلتر کردن خودکار پرس‌وجو و موجودیت‌های آگاه از tenant ارائه می‌دهد.

## الگوهای Multi-Tenancy

### 1. پایگاه داده برای هر Tenant

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

### 2. Schema برای هر Tenant

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

### 3. پایگاه داده مشترک با Tenant ID

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
│  │  │   Tenant    │  │   Query      │  │   Security   │ │  │
│  │  │   Filter    │  │   Filter    │  │   & Access   │ │  │
│  │  │             │  │             │  │              │ │  │
│  │  │ • Context   │  │ • WHERE     │  │ • Row Level  │ │  │
│  │  │ • Router    │  │   tenant_id │  │   Security   │ │  │
│  │  │ • Provider  │  │ • Global    │  │ • Permissions│ │  │
│  │  └─────────────┘  └─────────────┘  └──────────────┘ │  │
│  └─────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────┘
```

## شناسایی Tenant

### 1. رابط Tenant Provider

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

        // تلاش برای دریافت tenant از منابع مختلف
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
        var tenantId = ExtractTenantFromRequest(context);
        
        if (!string.IsNullOrEmpty(tenantId))
        {
            tenantProvider.SetCurrentTenant(tenantId);
            _logger.LogDebug("Tenant {TenantId} identified for request", tenantId);
        }
        else
        {
            _logger.LogWarning("No tenant identified for request");
        }

        await _next(context);
    }

    private string? ExtractTenantFromRequest(HttpContext context)
    {
        // 1. از subdomain استخراج کنید (مثل tenant1.example.com)
        var host = context.Request.Host.Host;
        var subdomain = ExtractSubdomain(host);
        if (!string.IsNullOrEmpty(subdomain))
        {
            return subdomain;
        }

        // 2. از مسیر استخراج کنید (مثل /tenant1/api/users)
        var pathSegments = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments?.Length > 0 && pathSegments[0].StartsWith("tenant"))
        {
            return pathSegments[0];
        }

        // 3. از header استخراج کنید
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader))
        {
            return tenantHeader.FirstOrDefault();
        }

        // 4. از query parameter استخراج کنید
        if (context.Request.Query.TryGetValue("tenantId", out var tenantQuery))
        {
            return tenantQuery.FirstOrDefault();
        }

        return null;
    }

    private string? ExtractSubdomain(string host)
    {
        var parts = host.Split('.');
        if (parts.Length > 2)
        {
            return parts[0];
        }
        return null;
    }
}
```

### 3. Tenant Validation

```csharp
public interface ITenantValidator
{
    Task<bool> IsValidTenantAsync(string tenantId);
    Task<TenantInfo?> GetTenantInfoAsync(string tenantId);
}

public class TenantInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
}

public class TenantValidator : ITenantValidator
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<TenantValidator> _logger;

    public TenantValidator(ITenantRepository tenantRepository, ILogger<TenantValidator> logger)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<bool> IsValidTenantAsync(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId))
            return false;

        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            return tenant != null && tenant.IsActive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating tenant {TenantId}", tenantId);
            return false;
        }
    }

    public async Task<TenantInfo?> GetTenantInfoAsync(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId))
            return null;

        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant == null || !tenant.IsActive)
                return null;

            return new TenantInfo
            {
                Id = tenant.Id,
                Name = tenant.Name,
                IsActive = tenant.IsActive,
                Settings = tenant.Settings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant info for {TenantId}", tenantId);
            return null;
        }
    }
}
```

## جداسازی داده

### 1. Tenant-Aware DbContext

```csharp
public class TenantAwareDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TenantAwareDbContext> _logger;

    public TenantAwareDbContext(
        DbContextOptions<TenantAwareDbContext> options,
        ITenantProvider tenantProvider,
        ILogger<TenantAwareDbContext> logger) : base(options)
    {
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // تنظیم فیلترهای tenant
        modelBuilder.Entity<User>().HasQueryFilter(u => u.TenantId == _tenantProvider.GetCurrentTenant());
        modelBuilder.Entity<Product>().HasQueryFilter(p => p.TenantId == _tenantProvider.GetCurrentTenant());
        modelBuilder.Entity<Order>().HasQueryFilter(o => o.TenantId == _tenantProvider.GetCurrentTenant());

        // تنظیم ایندکس‌ها
        modelBuilder.Entity<User>()
            .HasIndex(u => new { u.TenantId, u.Email })
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasIndex(p => new { p.TenantId, p.Sku })
            .IsUnique();
    }

    public override int SaveChanges()
    {
        SetTenantId();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTenantId();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void SetTenantId()
    {
        var currentTenant = _tenantProvider.GetCurrentTenant();
        if (string.IsNullOrEmpty(currentTenant)) return;

        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added && e.Entity is ITenantEntity);

        foreach (var entry in entries)
        {
            if (entry.Entity is ITenantEntity tenantEntity)
            {
                tenantEntity.TenantId = currentTenant;
                _logger.LogDebug("Set tenant {TenantId} for entity {EntityType}", 
                    currentTenant, entry.Entity.GetType().Name);
            }
        }
    }
}
```

### 2. Tenant Query Filter

```csharp
public class TenantQueryFilter<TEntity> : IQueryFilter<TEntity> 
    where TEntity : class, ITenantEntity
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
        var currentTenant = _tenantProvider.GetCurrentTenant();
        
        if (string.IsNullOrEmpty(currentTenant))
        {
            _logger.LogWarning("No tenant context available for query filtering");
            return query.Where(e => false); // بازگرداندن نتیجه خالی
        }

        _logger.LogDebug("Applying tenant filter for tenant {TenantId}", currentTenant);
        return query.Where(e => e.TenantId == currentTenant);
    }
}
```

## موجودیت‌های آگاه از Tenant

### 1. رابط Tenant Entity

```csharp
using Raziee.SharedKernel.MultiTenancy;

public interface ITenantEntity
{
    string TenantId { get; set; }
}

public class User : Entity<Guid>, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User(Guid id, string tenantId, string firstName, string lastName, string email) : base(id)
    {
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}

public class Product : Entity<Guid>, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Product(Guid id, string tenantId, string name, string description, string sku, decimal price, int stockQuantity) : base(id)
    {
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Sku = sku ?? throw new ArgumentNullException(nameof(sku));
        Price = price;
        StockQuantity = stockQuantity;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}

public class Order : Entity<Guid>, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();

    public Order(Guid id, string tenantId, Guid customerId) : base(id)
    {
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        CustomerId = customerId;
        Status = OrderStatus.Pending;
        TotalAmount = 0;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
```

### 2. Tenant-Aware Repository

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

        var currentTenant = _tenantProvider.GetCurrentTenant();
        if (!string.IsNullOrEmpty(currentTenant) && entity.TenantId != currentTenant)
        {
            _logger.LogWarning("Entity {EntityId} belongs to different tenant {EntityTenantId}, current tenant: {CurrentTenant}", 
                id, entity.TenantId, currentTenant);
            return null;
        }

        return entity;
    }

    public override async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var currentTenant = _tenantProvider.GetCurrentTenant();
        if (string.IsNullOrEmpty(currentTenant))
        {
            _logger.LogWarning("No tenant context available for query");
            return new List<TEntity>();
        }

        return await _dbSet.Where(e => e.TenantId == currentTenant).ToListAsync(cancellationToken);
    }

    public override async Task<IEnumerable<TEntity>> GetAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        var currentTenant = _tenantProvider.GetCurrentTenant();
        if (string.IsNullOrEmpty(currentTenant))
        {
            _logger.LogWarning("No tenant context available for query");
            return new List<TEntity>();
        }

        var query = ApplySpecification(specification);
        return await query.Where(e => e.TenantId == currentTenant).ToListAsync(cancellationToken);
    }

    public override async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var currentTenant = _tenantProvider.GetCurrentTenant();
        if (string.IsNullOrEmpty(currentTenant))
        {
            throw new InvalidOperationException("No tenant context available for adding entity");
        }

        entity.TenantId = currentTenant;
        await base.AddAsync(entity, cancellationToken);
        
        _logger.LogDebug("Added entity {EntityType} with tenant {TenantId}", 
            typeof(TEntity).Name, currentTenant);
    }
}
```

## فیلتر کردن پرس‌وجو

### 1. Global Query Filters

```csharp
public class TenantQueryFilterInterceptor : IInterceptor
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TenantQueryFilterInterceptor> _logger;

    public TenantQueryFilterInterceptor(ITenantProvider tenantProvider, ILogger<TenantQueryFilterInterceptor> logger)
    {
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public void Intercept(IInvocation invocation)
    {
        if (invocation.Method.Name == "ExecuteAsync" && 
            invocation.Arguments[0] is string sql)
        {
            var currentTenant = _tenantProvider.GetCurrentTenant();
            if (!string.IsNullOrEmpty(currentTenant))
            {
                // افزودن فیلتر tenant به SQL
                var modifiedSql = AddTenantFilterToSql(sql, currentTenant);
                invocation.Arguments[0] = modifiedSql;
                
                _logger.LogDebug("Applied tenant filter to SQL query for tenant {TenantId}", currentTenant);
            }
        }

        invocation.Proceed();
    }

    private string AddTenantFilterToSql(string sql, string tenantId)
    {
        // پیاده‌سازی منطق افزودن فیلتر tenant به SQL
        // این یک مثال ساده است - در عمل باید پیچیده‌تر باشد
        return sql.Replace("WHERE", $"WHERE tenant_id = '{tenantId}' AND");
    }
}
```

### 2. Specification با Tenant Filter

```csharp
public class TenantAwareSpecification<TEntity> : BaseSpecification<TEntity> 
    where TEntity : class, ITenantEntity
{
    private readonly ITenantProvider _tenantProvider;

    public TenantAwareSpecification(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
        AddTenantFilter();
    }

    private void AddTenantFilter()
    {
        var currentTenant = _tenantProvider.GetCurrentTenant();
        if (!string.IsNullOrEmpty(currentTenant))
        {
            AddCriteria(e => e.TenantId == currentTenant);
        }
    }
}

public class UsersByTenantSpecification : TenantAwareSpecification<User>
{
    public UsersByTenantSpecification(ITenantProvider tenantProvider) : base(tenantProvider)
    {
    }
}

public class ActiveUsersByTenantSpecification : TenantAwareSpecification<User>
{
    public ActiveUsersByTenantSpecification(ITenantProvider tenantProvider) : base(tenantProvider)
    {
        AddCriteria(u => u.IsActive);
        ApplyOrderBy(u => u.CreatedAt, OrderByDirection.Descending);
    }
}
```

## مثال کامل: برنامه SaaS

### 1. Tenant Management

```csharp
public class Tenant : Entity<string>
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
    public List<TenantUser> Users { get; set; } = new();

    public Tenant(string id, string name, string displayName) : base(id)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}

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

    public async Task<string> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating tenant {TenantName}", request.Name);

        // بررسی وجود tenant
        var existingTenant = await _tenantRepository.GetByIdAsync(request.Name, cancellationToken);
        if (existingTenant != null)
            throw new InvalidOperationException($"Tenant {request.Name} already exists");

        var tenant = new Tenant(request.Name, request.Name, request.DisplayName);
        await _tenantRepository.AddAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tenant {TenantId} created successfully", tenant.Id);
        return tenant.Id;
    }

    public async Task<TenantDto> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
            throw new InvalidOperationException($"Tenant {tenantId} not found");

        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            DisplayName = tenant.DisplayName,
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt,
            Settings = tenant.Settings
        };
    }
}
```

### 2. User Management

```csharp
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
        var currentTenant = _tenantProvider.GetCurrentTenant();
        if (string.IsNullOrEmpty(currentTenant))
            throw new InvalidOperationException("No tenant context available");

        _logger.LogInformation("Creating user {Email} for tenant {TenantId}", request.Email, currentTenant);

        // بررسی وجود کاربر
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
            throw new InvalidOperationException($"User with email {request.Email} already exists");

        var user = new User(Guid.NewGuid(), currentTenant, request.FirstName, request.LastName, request.Email);
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} created successfully for tenant {TenantId}", user.Id, currentTenant);
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
            Email = user.Email,
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
            Email = u.Email,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        });
    }
}
```

### 3. API Controllers

```csharp
[ApiController]
[Route("api/tenants/{tenantId}/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        ITenantProvider tenantProvider,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateUser(CreateUserRequest request)
    {
        // تنظیم tenant از route parameter
        var tenantId = RouteData.Values["tenantId"]?.ToString();
        if (string.IsNullOrEmpty(tenantId))
            return BadRequest("Tenant ID is required");

        _tenantProvider.SetCurrentTenant(tenantId);

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

## بهترین شیوه‌ها

### 1. امنیت Tenant
- همیشه tenant context را اعتبارسنجی کنید
- از فیلترهای خودکار استفاده کنید
- دسترسی cross-tenant را مسدود کنید
- از logging مناسب استفاده کنید

### 2. عملکرد
- از ایندکس‌های مناسب استفاده کنید
- فیلترهای tenant را بهینه کنید
- از caching استراتژیک استفاده کنید
- پرس‌وجوهای غیرضروری را کاهش دهید

### 3. مدیریت داده
- از soft delete استفاده کنید
- backup و restore را در نظر بگیرید
- migration strategies را برنامه‌ریزی کنید
- data retention policies را پیاده‌سازی کنید

### 4. تست
- tenant isolation را تست کنید
- از test data مناسب استفاده کنید
- integration tests را پیاده‌سازی کنید
- security tests را انجام دهید

### 5. Monitoring
- tenant-specific metrics را پیگیری کنید
- performance per tenant را نظارت کنید
- error tracking را پیاده‌سازی کنید
- alerting مناسب تنظیم کنید

این راهنما پایه جامعی برای پیاده‌سازی multi-tenancy با Raziee.SharedKernel ارائه می‌دهد، شامل تمام الگوها و شیوه‌های لازم برای ساخت برنامه‌های SaaS قابل اعتماد و مقیاس‌پذیر.
