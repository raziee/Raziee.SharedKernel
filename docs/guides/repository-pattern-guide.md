# Repository Pattern Guide

This comprehensive guide demonstrates how to use Raziee.SharedKernel to implement the Repository pattern effectively in your .NET applications.

## Table of Contents

- [Introduction](#introduction)
- [Repository Pattern Fundamentals](#repository-pattern-fundamentals)
- [Generic Repository Implementation](#generic-repository-implementation)
- [Specification Pattern](#specification-pattern)
- [Unit of Work Pattern](#unit-of-work-pattern)
- [Advanced Repository Features](#advanced-repository-features)
- [Complete Example: E-Commerce System](#complete-example-e-commerce-system)
- [Best Practices](#best-practices)

## Introduction

The Repository pattern provides an abstraction layer between the domain and data mapping layers, acting as an in-memory collection of domain objects. Raziee.SharedKernel provides a comprehensive implementation of this pattern with Entity Framework Core integration.

## Repository Pattern Fundamentals

### 1. Basic Repository Interface

```csharp
using Raziee.SharedKernel.Repositories;

public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<TEntity?> GetFirstOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync<TId>(TId id, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task DeleteByIdAsync<TId>(TId id, CancellationToken cancellationToken = default);
    Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<TEntity>> GetPagedAsync(ISpecification<TEntity> specification, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
```

### 2. Read-Only Repository Interface

```csharp
public interface IReadRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<TEntity?> GetFirstOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync<TId>(TId id, CancellationToken cancellationToken = default);
    Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<TEntity>> GetPagedAsync(ISpecification<TEntity> specification, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
```

## Generic Repository Implementation

### 1. Entity Framework Repository

```csharp
using Raziee.SharedKernel.Repositories;
using Raziee.SharedKernel.Specifications;
using Microsoft.EntityFrameworkCore;

public class EfRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public EfRepository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id! }, cancellationToken);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> GetFirstOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(cancellationToken);
    }

    public virtual async Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).CountAsync(cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).AnyAsync(cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync<TId>(TId id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id! }, cancellationToken) != null;
    }

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        await Task.CompletedTask;
    }

    public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        _dbSet.UpdateRange(entities);
        await Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        await Task.CompletedTask;
    }

    public virtual async Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        _dbSet.RemoveRange(entities);
        await Task.CompletedTask;
    }

    public virtual async Task DeleteByIdAsync<TId>(TId id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            await DeleteAsync(entity, cancellationToken);
        }
    }

    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await CountAsync(cancellationToken);
        var items = await _dbSet
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(ISpecification<TEntity> specification, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    protected virtual IQueryable<TEntity> ApplySpecification(ISpecification<TEntity> specification)
    {
        return SpecificationEvaluator<TEntity>.GetQuery(_dbSet, specification);
    }
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
```

### 2. Read-Only Repository Implementation

```csharp
public class EfReadRepository<TEntity> : EfRepository<TEntity>, IReadRepository<TEntity> where TEntity : class
{
    public EfReadRepository(DbContext context) : base(context)
    {
    }

    // Inherits all read operations from EfRepository
    // Write operations are not available in read-only repository
}
```

## Specification Pattern

### 1. Base Specification

```csharp
using Raziee.SharedKernel.Specifications;

public class UserByEmailSpecification : BaseSpecification<User, Guid>
{
    public UserByEmailSpecification(string email)
    {
        AddCriteria(u => u.Email.Value == email);
    }
}

public class ActiveUsersSpecification : BaseSpecification<User, Guid>
{
    public ActiveUsersSpecification()
    {
        AddCriteria(u => u.IsActive);
        ApplyOrderBy(u => u.CreatedAt, OrderByDirection.Descending);
    }
}

public class UsersByRoleSpecification : BaseSpecification<User, Guid>
{
    public UsersByRoleSpecification(string role)
    {
        AddCriteria(u => u.Role == role);
        AddInclude(u => u.Profile);
        ApplyOrderBy(u => u.LastName);
    }
}

public class UsersWithPaginationSpecification : BaseSpecification<User, Guid>
{
    public UsersWithPaginationSpecification(int pageNumber, int pageSize)
    {
        ApplyPaging(pageNumber, pageSize);
        ApplyOrderBy(u => u.CreatedAt, OrderByDirection.Descending);
    }
}

public class UsersSearchSpecification : BaseSpecification<User, Guid>
{
    public UsersSearchSpecification(string searchTerm)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            AddCriteria(u => u.FirstName.Contains(searchTerm) || 
                           u.LastName.Contains(searchTerm) || 
                           u.Email.Value.Contains(searchTerm));
        }
        
        ApplyOrderBy(u => u.LastName);
    }
}
```

### 2. Complex Specifications

```csharp
public class OrderByCustomerSpecification : BaseSpecification<Order, Guid>
{
    public OrderByCustomerSpecification(Guid customerId)
    {
        AddCriteria(o => o.CustomerId == customerId);
        AddInclude(o => o.Items);
        ApplyOrderBy(o => o.CreatedAt, OrderByDirection.Descending);
    }
}

public class OrderByStatusSpecification : BaseSpecification<Order, Guid>
{
    public OrderByStatusSpecification(OrderStatus status)
    {
        AddCriteria(o => o.Status == status);
        AddInclude(o => o.Customer);
        AddInclude(o => o.Items);
        ApplyOrderBy(o => o.CreatedAt, OrderByDirection.Descending);
    }
}

public class OrderByDateRangeSpecification : BaseSpecification<Order, Guid>
{
    public OrderByDateRangeSpecification(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        AddCriteria(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate);
        AddInclude(o => o.Customer);
        AddInclude(o => o.Items);
        ApplyOrderBy(o => o.CreatedAt, OrderByDirection.Descending);
    }
}

public class OrderByTotalAmountSpecification : BaseSpecification<Order, Guid>
{
    public OrderByTotalAmountSpecification(decimal minAmount, decimal maxAmount)
    {
        AddCriteria(o => o.TotalAmount.Amount >= minAmount && o.TotalAmount.Amount <= maxAmount);
        AddInclude(o => o.Customer);
        AddInclude(o => o.Items);
        ApplyOrderBy(o => o.TotalAmount.Amount, OrderByDirection.Descending);
    }
}

public class OrderWithItemsSpecification : BaseSpecification<Order, Guid>
{
    public OrderWithItemsSpecification(Guid orderId)
    {
        AddCriteria(o => o.Id == orderId);
        AddInclude(o => o.Items);
        AddInclude(o => o.Customer);
    }
}
```

### 3. Specification Composition

```csharp
public class ComplexOrderSpecification : BaseSpecification<Order, Guid>
{
    public ComplexOrderSpecification(
        Guid? customerId = null,
        OrderStatus? status = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        string? searchTerm = null)
    {
        if (customerId.HasValue)
            AddCriteria(o => o.CustomerId == customerId.Value);

        if (status.HasValue)
            AddCriteria(o => o.Status == status.Value);

        if (startDate.HasValue)
            AddCriteria(o => o.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            AddCriteria(o => o.CreatedAt <= endDate.Value);

        if (minAmount.HasValue)
            AddCriteria(o => o.TotalAmount.Amount >= minAmount.Value);

        if (maxAmount.HasValue)
            AddCriteria(o => o.TotalAmount.Amount <= maxAmount.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            AddCriteria(o => o.Customer.FirstName.Contains(searchTerm) ||
                           o.Customer.LastName.Contains(searchTerm) ||
                           o.Customer.Email.Value.Contains(searchTerm));
        }

        AddInclude(o => o.Customer);
        AddInclude(o => o.Items);
        ApplyOrderBy(o => o.CreatedAt, OrderByDirection.Descending);
    }
}
```

## Unit of Work Pattern

### 1. Unit of Work Implementation

```csharp
using Raziee.SharedKernel.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly ILogger<UnitOfWork> _logger;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(
        DbContext context,
        IDomainEventDispatcher domainEventDispatcher,
        ILogger<UnitOfWork> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _domainEventDispatcher = domainEventDispatcher ?? throw new ArgumentNullException(nameof(domainEventDispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Saving changes to database");

        // Dispatch domain events before saving
        await DispatchDomainEventsAsync(cancellationToken);

        var result = await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogDebug("Saved {Count} changes to database", result);
        return result;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            _logger.LogWarning("Transaction is already active");
            return;
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _logger.LogDebug("Transaction started");
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            _logger.LogWarning("No active transaction to commit");
            return;
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
            _logger.LogDebug("Transaction committed");
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            _logger.LogWarning("No active transaction to rollback");
            return;
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
            _logger.LogDebug("Transaction rolled back");
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public bool HasActiveTransaction => _transaction != null;

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var entitiesWithEvents = _context.ChangeTracker
            .Entries()
            .Where(e => e.Entity is IAggregateRoot)
            .Select(e => (IAggregateRoot)e.Entity)
            .Where(e => e.HasDomainEvents())
            .ToList();

        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Clear domain events from entities
        entitiesWithEvents.ForEach(e => e.ClearDomainEvents());

        // Dispatch domain events
        if (domainEvents.Any())
        {
            _logger.LogDebug("Dispatching {Count} domain events", domainEvents.Count);
            await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
        }
        await _context.DisposeAsync();
    }
}
```

### 2. Unit of Work Usage

```csharp
public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // Create order
            var order = new Order(Guid.NewGuid(), request.CustomerId);

            // Add items to order
            foreach (var item in request.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
                if (product == null)
                    throw new InvalidOperationException($"Product {item.ProductId} not found");

                if (product.StockQuantity < item.Quantity)
                    throw new InvalidOperationException($"Insufficient stock for product {item.ProductId}");

                order.AddItem(product, item.Quantity);
                
                // Update product stock
                product.UpdateStock(product.StockQuantity - item.Quantity);
                await _productRepository.UpdateAsync(product, cancellationToken);
            }

            // Save order
            await _orderRepository.AddAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} created successfully", order.Id);
            return order.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order for customer {CustomerId}", request.CustomerId);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
```

## Advanced Repository Features

### 1. Soft Delete Support

```csharp
public interface ISoftDelete
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAt { get; }
    string? DeletedBy { get; }
}

public class SoftDeleteRepository<TEntity> : EfRepository<TEntity> 
    where TEntity : class, ISoftDelete
{
    public SoftDeleteRepository(DbContext context) : base(context)
    {
    }

    public override async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => !e.IsDeleted).ToListAsync(cancellationToken);
    }

    public override async Task<IEnumerable<TEntity>> GetAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        return await query.Where(e => !e.IsDeleted).ToListAsync(cancellationToken);
    }

    public override async Task<TEntity?> GetFirstOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        return await query.Where(e => !e.IsDeleted).FirstOrDefaultAsync(cancellationToken);
    }

    public override async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => !e.IsDeleted).CountAsync(cancellationToken);
    }

    public override async Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        return await query.Where(e => !e.IsDeleted).CountAsync(cancellationToken);
    }

    public override async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => !e.IsDeleted).AnyAsync(cancellationToken);
    }

    public override async Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        return await query.Where(e => !e.IsDeleted).AnyAsync(cancellationToken);
    }

    public virtual async Task SoftDeleteAsync(TEntity entity, string deletedBy, CancellationToken cancellationToken = default)
    {
        entity.GetType().GetProperty("IsDeleted")?.SetValue(entity, true);
        entity.GetType().GetProperty("DeletedAt")?.SetValue(entity, DateTimeOffset.UtcNow);
        entity.GetType().GetProperty("DeletedBy")?.SetValue(entity, deletedBy);
        
        await UpdateAsync(entity, cancellationToken);
    }

    public virtual async Task SoftDeleteByIdAsync<TId>(TId id, string deletedBy, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            await SoftDeleteAsync(entity, deletedBy, cancellationToken);
        }
    }
}
```

### 2. Audit Support

```csharp
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; }
    string? CreatedBy { get; }
    DateTimeOffset? UpdatedAt { get; }
    string? UpdatedBy { get; }
}

public class AuditableRepository<TEntity> : EfRepository<TEntity> 
    where TEntity : class, IAuditableEntity
{
    private readonly ICurrentUserService _currentUserService;

    public AuditableRepository(DbContext context, ICurrentUserService currentUserService) : base(context)
    {
        _currentUserService = currentUserService;
    }

    public override async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        entity.GetType().GetProperty("CreatedAt")?.SetValue(entity, DateTimeOffset.UtcNow);
        entity.GetType().GetProperty("CreatedBy")?.SetValue(entity, currentUser);
        
        await base.AddAsync(entity, cancellationToken);
    }

    public override async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        entity.GetType().GetProperty("UpdatedAt")?.SetValue(entity, DateTimeOffset.UtcNow);
        entity.GetType().GetProperty("UpdatedBy")?.SetValue(entity, currentUser);
        
        await base.UpdateAsync(entity, cancellationToken);
    }
}
```

### 3. Multi-Tenancy Support

```csharp
public interface ITenantEntity
{
    string TenantId { get; }
}

public class TenantRepository<TEntity> : EfRepository<TEntity> 
    where TEntity : class, ITenantEntity
{
    private readonly ITenantProvider _tenantProvider;

    public TenantRepository(DbContext context, ITenantProvider tenantProvider) : base(context)
    {
        _tenantProvider = tenantProvider;
    }

    protected override IQueryable<TEntity> ApplySpecification(ISpecification<TEntity> specification)
    {
        var query = base.ApplySpecification(specification);
        var tenantId = _tenantProvider.GetCurrentTenant();
        
        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(e => e.TenantId == tenantId);
        }
        
        return query;
    }

    public override async Task<TEntity?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default)
    {
        var entity = await base.GetByIdAsync(id, cancellationToken);
        if (entity == null) return null;

        var tenantId = _tenantProvider.GetCurrentTenant();
        if (!string.IsNullOrEmpty(tenantId) && entity.TenantId != tenantId)
        {
            return null; // Entity belongs to different tenant
        }

        return entity;
    }
}
```

## Complete Example: E-Commerce System

### 1. Domain Entities

```csharp
public class Customer : AggregateRoot<Guid>
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Email Email { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Customer(Guid id, string firstName, string lastName, Email email) : base(id)
    {
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

### 2. Repository Implementations

```csharp
public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm, CancellationToken cancellationToken = default);
}

public class EfCustomerRepository : EfRepository<Customer>, ICustomerRepository
{
    public EfCustomerRepository(DbContext context) : base(context)
    {
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var specification = new CustomerByEmailSpecification(email);
        return await GetFirstOrDefaultAsync(specification, cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default)
    {
        var specification = new ActiveCustomersSpecification();
        return await GetAsync(specification, cancellationToken);
    }

    public async Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var specification = new CustomerSearchSpecification(searchTerm);
        return await GetAsync(specification, cancellationToken);
    }
}

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm, CancellationToken cancellationToken = default);
}

public class EfProductRepository : EfRepository<Product>, IProductRepository
{
    public EfProductRepository(DbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default)
    {
        var specification = new ActiveProductsSpecification();
        return await GetAsync(specification, cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        var specification = new ProductsByCategorySpecification(category);
        return await GetAsync(specification, cancellationToken);
    }

    public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var specification = new ProductSearchSpecification(searchTerm);
        return await GetAsync(specification, cancellationToken);
    }
}

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
```

### 3. Service Layer

```csharp
public class CustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        ILogger<CustomerService> logger)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating customer with email {Email}", request.Email);

        // Check if customer already exists
        var existingCustomer = await _customerRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingCustomer != null)
            throw new InvalidOperationException("Customer with this email already exists");

        var customer = new Customer(Guid.NewGuid(), request.FirstName, request.LastName, new Email(request.Email));
        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Customer {CustomerId} created successfully", customer.Id);
        return customer.Id;
    }

    public async Task<CustomerDto> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null)
            throw new InvalidOperationException($"Customer {customerId} not found");

        return new CustomerDto
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email.Value,
            IsActive = customer.IsActive,
            CreatedAt = customer.CreatedAt
        };
    }

    public async Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var customers = await _customerRepository.SearchCustomersAsync(searchTerm, cancellationToken);
        return customers.Select(c => new CustomerDto
        {
            Id = c.Id,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Email = c.Email.Value,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt
        });
    }
}

public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

        // Validate customer exists
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer == null)
            throw new InvalidOperationException($"Customer {request.CustomerId} not found");

        if (!customer.IsActive)
            throw new InvalidOperationException("Customer is not active");

        // Create order
        var order = new Order(Guid.NewGuid(), request.CustomerId);

        // Add items to order
        foreach (var item in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            if (product == null)
                throw new InvalidOperationException($"Product {item.ProductId} not found");

            if (!product.IsActive)
                throw new InvalidOperationException($"Product {item.ProductId} is not active");

            if (product.StockQuantity < item.Quantity)
                throw new InvalidOperationException($"Insufficient stock for product {item.ProductId}");

            order.AddItem(product, item.Quantity);
        }

        // Save order
        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} created successfully", order.Id);
        return order.Id;
    }

    public async Task<OrderDto> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetOrderWithItemsAsync(orderId, cancellationToken);
        if (order == null)
            throw new InvalidOperationException($"Order {orderId} not found");

        return new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount.Amount,
            Currency = order.TotalAmount.Currency,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice.Amount
            }).ToList()
        };
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetOrdersByCustomerAsync(customerId, cancellationToken);
        return orders.Select(o => new OrderDto
        {
            Id = o.Id,
            CustomerId = o.CustomerId,
            Status = o.Status.ToString(),
            TotalAmount = o.TotalAmount.Amount,
            Currency = o.TotalAmount.Currency,
            CreatedAt = o.CreatedAt,
            Items = o.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice.Amount
            }).ToList()
        });
    }
}
```

## Best Practices

### 1. Repository Design
- Keep repositories focused on data access
- Use specifications for complex queries
- Implement proper error handling
- Use async/await consistently

### 2. Specification Pattern
- Create reusable specifications
- Combine specifications for complex queries
- Use meaningful names for specifications
- Implement proper null handling

### 3. Unit of Work
- Use transactions for data consistency
- Handle exceptions properly
- Implement proper disposal
- Use domain events for side effects

### 4. Performance Considerations
- Use appropriate include strategies
- Implement pagination for large datasets
- Use read-only repositories for queries
- Implement proper caching strategies

### 5. Testing
- Mock repositories in unit tests
- Use in-memory databases for integration tests
- Test specifications thoroughly
- Implement proper test data setup

This guide provides a comprehensive foundation for implementing the Repository pattern with Raziee.SharedKernel, including all the necessary patterns and practices for building maintainable and testable data access layers.
