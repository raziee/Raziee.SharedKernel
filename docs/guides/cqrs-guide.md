# CQRS (Command Query Responsibility Segregation) Guide

This comprehensive guide demonstrates how to use Raziee.SharedKernel to implement CQRS patterns effectively in your .NET applications.

## Table of Contents

- [Introduction](#introduction)
- [CQRS Fundamentals](#cqrs-fundamentals)
- [Command Implementation](#command-implementation)
- [Query Implementation](#query-implementation)
- [Pipeline Behaviors](#pipeline-behaviors)
- [Validation](#validation)
- [Logging and Monitoring](#logging-and-monitoring)
- [Caching](#caching)
- [Transaction Management](#transaction-management)
- [Complete Example: E-Commerce System](#complete-example-e-commerce-system)
- [Best Practices](#best-practices)

## Introduction

CQRS (Command Query Responsibility Segregation) separates read and write operations into different models, allowing for optimized data access patterns and improved scalability. Raziee.SharedKernel provides a comprehensive implementation of CQRS with MediatR integration.

## CQRS Fundamentals

### 1. Command and Query Interfaces

```csharp
using Raziee.SharedKernel.CQRS;

// Commands - Write operations
public interface ICommand : IRequest { }
public interface ICommand<out TResponse> : ICommand, IRequest<TResponse> { }

// Queries - Read operations
public interface IQuery<out TResponse> : IRequest<TResponse> { }

// Handlers
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand { }

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse> { }

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse> { }
```

### 2. Pipeline Behaviors

```csharp
public interface IPipelineBehavior<in TRequest, TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
```

## Command Implementation

### 1. Basic Commands

```csharp
// Create Customer Command
public class CreateCustomerCommand : ICommand<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CreateCustomerCommandHandler : ICommandHandler<CreateCustomerCommand, Guid>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateCustomerCommandHandler> _logger;

    public CreateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateCustomerCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
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
}

// Update Customer Command
public class UpdateCustomerCommand : ICommand
{
    public Guid CustomerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class UpdateCustomerCommandHandler : ICommandHandler<UpdateCustomerCommand>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateCustomerCommandHandler> _logger;

    public UpdateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateCustomerCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating customer {CustomerId}", request.CustomerId);

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer == null)
            throw new InvalidOperationException($"Customer {request.CustomerId} not found");

        customer.UpdateName(request.FirstName, request.LastName);
        await _customerRepository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Customer {CustomerId} updated successfully", customer.Id);
    }
}

// Delete Customer Command
public class DeleteCustomerCommand : ICommand
{
    public Guid CustomerId { get; set; }
}

public class DeleteCustomerCommandHandler : ICommandHandler<DeleteCustomerCommand>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCustomerCommandHandler> _logger;

    public DeleteCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCustomerCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting customer {CustomerId}", request.CustomerId);

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer == null)
            throw new InvalidOperationException($"Customer {request.CustomerId} not found");

        await _customerRepository.DeleteAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Customer {CustomerId} deleted successfully", customer.Id);
    }
}
```

### 2. Complex Commands

```csharp
// Create Order Command
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

public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

        // Validate customer exists and is active
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
}

// Confirm Order Command
public class ConfirmOrderCommand : ICommand
{
    public Guid OrderId { get; set; }
}

public class ConfirmOrderCommandHandler : ICommandHandler<ConfirmOrderCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmOrderCommandHandler> _logger;

    public ConfirmOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        ILogger<ConfirmOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Confirming order {OrderId}", request.OrderId);

        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
            throw new InvalidOperationException($"Order {request.OrderId} not found");

        order.Confirm();
        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} confirmed successfully", order.Id);
    }
}
```

## Query Implementation

### 1. Basic Queries

```csharp
// Get Customer Query
public class GetCustomerQuery : IQuery<CustomerDto>
{
    public Guid CustomerId { get; set; }
}

public class CustomerDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class GetCustomerQueryHandler : IQueryHandler<GetCustomerQuery, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<GetCustomerQueryHandler> _logger;

    public GetCustomerQueryHandler(ICustomerRepository customerRepository, ILogger<GetCustomerQueryHandler> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task<CustomerDto> Handle(GetCustomerQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting customer {CustomerId}", request.CustomerId);

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer == null)
            throw new InvalidOperationException($"Customer {request.CustomerId} not found");

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
}

// Get All Customers Query
public class GetAllCustomersQuery : IQuery<IEnumerable<CustomerDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class GetAllCustomersQueryHandler : IQueryHandler<GetAllCustomersQuery, IEnumerable<CustomerDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<GetAllCustomersQueryHandler> _logger;

    public GetAllCustomersQueryHandler(ICustomerRepository customerRepository, ILogger<GetAllCustomersQueryHandler> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<CustomerDto>> Handle(GetAllCustomersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all customers with pagination");

        var specification = new CustomersWithPaginationSpecification(request.PageNumber, request.PageSize);
        var customers = await _customerRepository.GetAsync(specification, cancellationToken);

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
```

### 2. Complex Queries

```csharp
// Search Customers Query
public class SearchCustomersQuery : IQuery<IEnumerable<CustomerDto>>
{
    public string SearchTerm { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class SearchCustomersQueryHandler : IQueryHandler<SearchCustomersQuery, IEnumerable<CustomerDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<SearchCustomersQueryHandler> _logger;

    public SearchCustomersQueryHandler(ICustomerRepository customerRepository, ILogger<SearchCustomersQueryHandler> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<CustomerDto>> Handle(SearchCustomersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching customers with term: {SearchTerm}", request.SearchTerm);

        var specification = new CustomerSearchSpecification(request.SearchTerm, request.PageNumber, request.PageSize);
        var customers = await _customerRepository.GetAsync(specification, cancellationToken);

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

// Get Order Query
public class GetOrderQuery : IQuery<OrderDto>
{
    public Guid OrderId { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

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
        _logger.LogInformation("Getting order {OrderId}", request.OrderId);

        var order = await _orderRepository.GetOrderWithItemsAsync(request.OrderId, cancellationToken);
        if (order == null)
            throw new InvalidOperationException($"Order {request.OrderId} not found");

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
}

// Get Orders by Customer Query
public class GetOrdersByCustomerQuery : IQuery<IEnumerable<OrderDto>>
{
    public Guid CustomerId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class GetOrdersByCustomerQueryHandler : IQueryHandler<GetOrdersByCustomerQuery, IEnumerable<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<GetOrdersByCustomerQueryHandler> _logger;

    public GetOrdersByCustomerQueryHandler(IOrderRepository orderRepository, ILogger<GetOrdersByCustomerQueryHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<OrderDto>> Handle(GetOrdersByCustomerQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting orders for customer {CustomerId}", request.CustomerId);

        var orders = await _orderRepository.GetOrdersByCustomerAsync(request.CustomerId, cancellationToken);

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

## Pipeline Behaviors

### 1. Validation Behavior

```csharp
using Raziee.SharedKernel.CQRS;
using FluentValidation;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Validating request {RequestType}", typeof(TRequest).Name);

        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Any())
            {
                _logger.LogWarning("Validation failed for request {RequestType}: {Failures}", 
                    typeof(TRequest).Name, string.Join(", ", failures.Select(f => f.ErrorMessage)));
                throw new ValidationException(failures);
            }
        }

        _logger.LogDebug("Validation passed for request {RequestType}", typeof(TRequest).Name);
        return await next();
    }
}
```

### 2. Logging Behavior

```csharp
using Raziee.SharedKernel.CQRS;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid();

        _logger.LogInformation("Handling {RequestName} with ID {RequestId}", requestName, requestId);

        try
        {
            var response = await next();
            _logger.LogInformation("Successfully handled {RequestName} with ID {RequestId}", requestName, requestId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {RequestName} with ID {RequestId}", requestName, requestId);
            throw;
        }
    }
}
```

### 3. Transaction Behavior

```csharp
using Raziee.SharedKernel.CQRS;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(IUnitOfWork unitOfWork, ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid();

        _logger.LogInformation("Starting transaction for {RequestName} with ID {RequestId}", requestName, requestId);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            var response = await next();
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            
            _logger.LogInformation("Transaction committed for {RequestName} with ID {RequestId}", requestName, requestId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction failed for {RequestName} with ID {RequestId}, rolling back", requestName, requestId);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
```

### 4. Caching Behavior

```csharp
using Raziee.SharedKernel.CQRS;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(IMemoryCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Only cache queries
        if (request is not IQuery<TResponse>)
        {
            return await next();
        }

        var cacheKey = GenerateCacheKey(request);
        
        if (_cache.TryGetValue(cacheKey, out TResponse? cachedResponse))
        {
            _logger.LogDebug("Cache hit for {RequestType} with key {CacheKey}", typeof(TRequest).Name, cacheKey);
            return cachedResponse!;
        }

        _logger.LogDebug("Cache miss for {RequestType} with key {CacheKey}", typeof(TRequest).Name, cacheKey);
        var response = await next();
        
        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));
        _logger.LogDebug("Cached response for {RequestType} with key {CacheKey}", typeof(TRequest).Name, cacheKey);
        
        return response;
    }

    private string GenerateCacheKey(TRequest request)
    {
        var requestType = typeof(TRequest).Name;
        var requestJson = JsonSerializer.Serialize(request);
        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(requestJson)));
        return $"{requestType}:{requestHash}";
    }
}
```

## Validation

### 1. FluentValidation Validators

```csharp
using FluentValidation;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters");
    }
}

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");
    }
}

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must have at least one item")
            .Must(items => items.All(item => item.Quantity > 0))
            .WithMessage("All items must have a positive quantity");

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(x => x.ProductId)
                    .NotEmpty().WithMessage("Product ID is required");
                
                item.RuleFor(x => x.Quantity)
                    .GreaterThan(0).WithMessage("Quantity must be greater than 0");
            });
    }
}

public class GetCustomerQueryValidator : AbstractValidator<GetCustomerQuery>
{
    public GetCustomerQueryValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required");
    }
}
```

## Logging and Monitoring

### 1. Structured Logging

```csharp
public class CustomerService
{
    private readonly IMediator _mediator;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(IMediator mediator, ILogger<CustomerService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Guid> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating customer with email {Email}", request.Email);

        var command = new CreateCustomerCommand
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email
        };

        try
        {
            var customerId = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Customer {CustomerId} created successfully", customerId);
            return customerId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create customer with email {Email}", request.Email);
            throw;
        }
    }

    public async Task<CustomerDto> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting customer {CustomerId}", customerId);

        var query = new GetCustomerQuery { CustomerId = customerId };
        return await _mediator.Send(query, cancellationToken);
    }
}
```

### 2. Performance Monitoring

```csharp
public class PerformanceMonitoringBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceMonitoringBehavior<TRequest, TResponse>> _logger;

    public PerformanceMonitoringBehavior(ILogger<PerformanceMonitoringBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();
            
            _logger.LogInformation("Request {RequestName} completed in {ElapsedMilliseconds}ms", 
                requestName, stopwatch.ElapsedMilliseconds);
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Request {RequestName} failed after {ElapsedMilliseconds}ms", 
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

## Caching

### 1. Query Caching

```csharp
public class CachedGetCustomerQueryHandler : IQueryHandler<GetCustomerQuery, CustomerDto>
{
    private readonly IQueryHandler<GetCustomerQuery, CustomerDto> _handler;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedGetCustomerQueryHandler> _logger;

    public CachedGetCustomerQueryHandler(
        IQueryHandler<GetCustomerQuery, CustomerDto> handler,
        IMemoryCache cache,
        ILogger<CachedGetCustomerQueryHandler> logger)
    {
        _handler = handler;
        _cache = cache;
        _logger = logger;
    }

    public async Task<CustomerDto> Handle(GetCustomerQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"customer:{request.CustomerId}";
        
        if (_cache.TryGetValue(cacheKey, out CustomerDto? cachedCustomer))
        {
            _logger.LogDebug("Cache hit for customer {CustomerId}", request.CustomerId);
            return cachedCustomer!;
        }

        _logger.LogDebug("Cache miss for customer {CustomerId}", request.CustomerId);
        var customer = await _handler.Handle(request, cancellationToken);
        
        _cache.Set(cacheKey, customer, TimeSpan.FromMinutes(10));
        _logger.LogDebug("Cached customer {CustomerId}", request.CustomerId);
        
        return customer;
    }
}
```

### 2. Cache Invalidation

```csharp
public class CacheInvalidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheInvalidationBehavior<TRequest, TResponse>> _logger;

    public CacheInvalidationBehavior(IMemoryCache cache, ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();
        
        // Invalidate cache for commands that modify data
        if (request is ICommand)
        {
            InvalidateRelatedCache(request);
        }
        
        return response;
    }

    private void InvalidateRelatedCache(TRequest request)
    {
        var requestType = typeof(TRequest).Name;
        
        // Invalidate customer cache for customer-related commands
        if (requestType.Contains("Customer"))
        {
            _cache.Remove("customer:*");
            _logger.LogDebug("Invalidated customer cache for {RequestType}", requestType);
        }
        
        // Invalidate order cache for order-related commands
        if (requestType.Contains("Order"))
        {
            _cache.Remove("order:*");
            _logger.LogDebug("Invalidated order cache for {RequestType}", requestType);
        }
    }
}
```

## Transaction Management

### 1. Unit of Work Integration

```csharp
public class OrderService
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IMediator mediator, IUnitOfWork unitOfWork, ILogger<OrderService> logger)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var command = new CreateOrderCommand
            {
                CustomerId = request.CustomerId,
                Items = request.Items.Select(item => new OrderItemDto
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                }).ToList()
            };

            var orderId = await _mediator.Send(command, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} created successfully", orderId);
            return orderId;
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

## Complete Example: E-Commerce System

### 1. API Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(IMediator mediator, ILogger<CustomersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateCustomer(CreateCustomerRequest request)
    {
        var command = new CreateCustomerCommand
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email
        };

        var customerId = await _mediator.Send(command);
        return Ok(customerId);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetCustomer(Guid id)
    {
        var query = new GetCustomerQuery { CustomerId = id };
        var customer = await _mediator.Send(query);
        return Ok(customer);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetAllCustomersQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var customers = await _mediator.Send(query);
        return Ok(customers);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateCustomer(Guid id, UpdateCustomerRequest request)
    {
        var command = new UpdateCustomerCommand
        {
            CustomerId = id,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCustomer(Guid id)
    {
        var command = new DeleteCustomerCommand { CustomerId = id };
        await _mediator.Send(command);
        return NoContent();
    }
}

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

    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByCustomer(Guid customerId)
    {
        var query = new GetOrdersByCustomerQuery { CustomerId = customerId };
        var orders = await _mediator.Send(query);
        return Ok(orders);
    }

    [HttpPost("{id}/confirm")]
    public async Task<ActionResult> ConfirmOrder(Guid id)
    {
        var command = new ConfirmOrderCommand { OrderId = id };
        await _mediator.Send(command);
        return NoContent();
    }
}
```

### 2. Service Registration

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add Raziee.SharedKernel
        builder.Services.AddSharedKernel();

        // Add MediatR
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
        });

        // Add pipeline behaviors
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceMonitoringBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CacheInvalidationBehavior<,>));

        // Add FluentValidation
        builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

        // Add caching
        builder.Services.AddMemoryCache();

        var app = builder.Build();

        app.Run();
    }
}
```

## Best Practices

### 1. Command Design
- Keep commands focused on a single operation
- Use meaningful names for commands
- Include all necessary data in commands
- Implement proper validation

### 2. Query Design
- Keep queries focused on data retrieval
- Use DTOs for query results
- Implement proper pagination
- Use specifications for complex queries

### 3. Handler Design
- Keep handlers focused on a single responsibility
- Use dependency injection properly
- Implement proper error handling
- Use logging consistently

### 4. Pipeline Behaviors
- Use behaviors for cross-cutting concerns
- Implement proper error handling
- Use logging for monitoring
- Consider performance implications

### 5. Validation
- Use FluentValidation for complex validation
- Implement proper error messages
- Use validation for both commands and queries
- Consider business rule validation

This guide provides a comprehensive foundation for implementing CQRS with Raziee.SharedKernel, including all the necessary patterns and practices for building maintainable and scalable applications.
