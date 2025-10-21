# Distributed Transactions Guide

This comprehensive guide demonstrates how to use Raziee.SharedKernel to implement distributed transactions using the Saga pattern in your .NET applications.

## Table of Contents

- [Introduction](#introduction)
- [Distributed Transactions Overview](#distributed-transactions-overview)
- [Saga Pattern](#saga-pattern)
- [Saga Orchestrator](#saga-orchestrator)
- [Saga Steps](#saga-steps)
- [Saga State Management](#saga-state-management)
- [Error Handling and Compensation](#error-handling-and-compensation)
- [Complete Example: E-Commerce Order Processing](#complete-example-e-commerce-order-processing)
- [Best Practices](#best-practices)

## Introduction

Distributed transactions are essential for maintaining data consistency across multiple services in microservices architectures. The Saga pattern provides a way to manage long-running business processes that span multiple services while maintaining eventual consistency.

## Distributed Transactions Overview

### 1. The Problem

In microservices architectures, traditional ACID transactions are not feasible across service boundaries. This leads to challenges in maintaining data consistency when operations span multiple services.

### 2. The Solution: Saga Pattern

The Saga pattern provides a way to manage distributed transactions by:
- Breaking down complex operations into smaller, manageable steps
- Implementing compensation logic for rollback scenarios
- Maintaining eventual consistency across services
- Providing visibility into transaction state

## Saga Pattern

### 1. Saga Components

```csharp
using Raziee.SharedKernel.DistributedTransactions;

// Saga State
public class SagaState<TData> where TData : class
{
    public Guid Id { get; set; }
    public int CurrentStepIndex { get; set; }
    public TData Data { get; set; } = default!;
    public SagaStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
}

// Saga Status
public enum SagaStatus
{
    Pending,        // Saga is pending execution
    Running,        // Saga is currently executing
    Completed,      // Saga has completed successfully
    Compensating,   // Saga has failed and is being compensated
    Compensated,    // Saga has been fully compensated
    Failed,         // Saga has failed and cannot be compensated
}
```

### 2. Saga Step Base Class

```csharp
public abstract class SagaStep<TData> where TData : class
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    
    public abstract Task ExecuteAsync(TData data, CancellationToken cancellationToken = default);
    public abstract Task CompensateAsync(TData data, CancellationToken cancellationToken = default);
    
    public virtual bool CanExecute(TData data) => true;
    public virtual bool CanCompensate(TData data) => true;
}
```

## Saga Orchestrator

### 1. Interface Definition

```csharp
public interface ISagaOrchestrator
{
    Task StartSagaAsync<TData>(Guid sagaId, TData data, CancellationToken cancellationToken = default)
        where TData : class;
    
    Task ExecuteNextStepAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default)
        where TData : class;
    
    Task CompensateStepAsync<TData>(Guid sagaId, int stepIndex, CancellationToken cancellationToken = default)
        where TData : class;
    
    Task<SagaState<TData>?> GetSagaStateAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default)
        where TData : class;
}
```

### 2. Saga Orchestrator Implementation

```csharp
public class SagaOrchestrator : ISagaOrchestrator
{
    private readonly ISagaStore _sagaStore;
    private readonly ILogger<SagaOrchestrator> _logger;

    public SagaOrchestrator(ISagaStore sagaStore, ILogger<SagaOrchestrator> logger)
    {
        _sagaStore = sagaStore;
        _logger = logger;
    }

    public async Task StartSagaAsync<TData>(Guid sagaId, TData data, CancellationToken cancellationToken = default)
        where TData : class
    {
        _logger.LogInformation("Starting saga {SagaId}", sagaId);

        var sagaState = new SagaState<TData>
        {
            Id = sagaId,
            Data = data,
            Status = SagaStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _sagaStore.SaveSagaStateAsync(sagaState, cancellationToken);
        _logger.LogInformation("Saga {SagaId} started successfully", sagaId);
    }

    public async Task ExecuteNextStepAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default)
        where TData : class
    {
        _logger.LogInformation("Executing next step for saga {SagaId}", sagaId);

        var sagaState = await _sagaStore.GetSagaStateAsync<TData>(sagaId, cancellationToken);
        if (sagaState == null)
        {
            _logger.LogError("Saga {SagaId} not found", sagaId);
            throw new InvalidOperationException($"Saga {sagaId} not found");
        }

        if (sagaState.Status != SagaStatus.Pending && sagaState.Status != SagaStatus.Running)
        {
            _logger.LogWarning("Saga {SagaId} is not in a valid state for execution. Current status: {Status}", 
                sagaId, sagaState.Status);
            return;
        }

        try
        {
            sagaState.Status = SagaStatus.Running;
            sagaState.UpdatedAt = DateTimeOffset.UtcNow;
            await _sagaStore.SaveSagaStateAsync(sagaState, cancellationToken);

            // Get the next step to execute
            var nextStep = GetNextStep<TData>(sagaState);
            if (nextStep == null)
            {
                // No more steps, saga is complete
                sagaState.Status = SagaStatus.Completed;
                sagaState.UpdatedAt = DateTimeOffset.UtcNow;
                await _sagaStore.SaveSagaStateAsync(sagaState, cancellationToken);
                _logger.LogInformation("Saga {SagaId} completed successfully", sagaId);
                return;
            }

            // Execute the step
            if (nextStep.CanExecute(sagaState.Data))
            {
                await nextStep.ExecuteAsync(sagaState.Data, cancellationToken);
                sagaState.CurrentStepIndex++;
                sagaState.Status = SagaStatus.Pending;
                sagaState.UpdatedAt = DateTimeOffset.UtcNow;
                await _sagaStore.SaveSagaStateAsync(sagaState, cancellationToken);
                
                _logger.LogInformation("Step {StepName} executed successfully for saga {SagaId}", 
                    nextStep.Name, sagaId);
            }
            else
            {
                _logger.LogWarning("Step {StepName} cannot be executed for saga {SagaId}", 
                    nextStep.Name, sagaId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing step for saga {SagaId}", sagaId);
            sagaState.Status = SagaStatus.Failed;
            sagaState.Error = ex.Message;
            sagaState.UpdatedAt = DateTimeOffset.UtcNow;
            await _sagaStore.SaveSagaStateAsync(sagaState, cancellationToken);
            throw;
        }
    }

    public async Task CompensateStepAsync<TData>(Guid sagaId, int stepIndex, CancellationToken cancellationToken = default)
        where TData : class
    {
        _logger.LogInformation("Compensating step {StepIndex} for saga {SagaId}", stepIndex, sagaId);

        var sagaState = await _sagaStore.GetSagaStateAsync<TData>(sagaId, cancellationToken);
        if (sagaState == null)
        {
            _logger.LogError("Saga {SagaId} not found", sagaId);
            throw new InvalidOperationException($"Saga {sagaId} not found");
        }

        try
        {
            sagaState.Status = SagaStatus.Compensating;
            sagaState.UpdatedAt = DateTimeOffset.UtcNow;
            await _sagaStore.SaveSagaStateAsync(sagaState, cancellationToken);

            // Get the step to compensate
            var step = GetStep<TData>(sagaState, stepIndex);
            if (step != null && step.CanCompensate(sagaState.Data))
            {
                await step.CompensateAsync(sagaState.Data, cancellationToken);
                _logger.LogInformation("Step {StepName} compensated successfully for saga {SagaId}", 
                    step.Name, sagaId);
            }
            else
            {
                _logger.LogWarning("Step {StepIndex} cannot be compensated for saga {SagaId}", 
                    stepIndex, sagaId);
            }

            sagaState.Status = SagaStatus.Compensated;
            sagaState.UpdatedAt = DateTimeOffset.UtcNow;
            await _sagaStore.SaveSagaStateAsync(sagaState, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compensating step {StepIndex} for saga {SagaId}", stepIndex, sagaId);
            sagaState.Status = SagaStatus.Failed;
            sagaState.Error = ex.Message;
            sagaState.UpdatedAt = DateTimeOffset.UtcNow;
            await _sagaStore.SaveSagaStateAsync(sagaState, cancellationToken);
            throw;
        }
    }

    public async Task<SagaState<TData>?> GetSagaStateAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default)
        where TData : class
    {
        return await _sagaStore.GetSagaStateAsync<TData>(sagaId, cancellationToken);
    }

    private SagaStep<TData>? GetNextStep<TData>(SagaState<TData> sagaState) where TData : class
    {
        // This would be implemented based on your specific saga logic
        // For now, return null to indicate no more steps
        return null;
    }

    private SagaStep<TData>? GetStep<TData>(SagaState<TData> sagaState, int stepIndex) where TData : class
    {
        // This would be implemented based on your specific saga logic
        // For now, return null
        return null;
    }
}
```

## Saga Steps

### 1. Basic Saga Step Implementation

```csharp
public class ValidateCustomerStep : SagaStep<OrderSagaData>
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<ValidateCustomerStep> _logger;

    public ValidateCustomerStep(ICustomerService customerService, ILogger<ValidateCustomerStep> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    public override string Name => "ValidateCustomer";
    public override string Description => "Validates that the customer exists and is active";

    public override async Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating customer {CustomerId} for order {OrderId}", 
            data.CustomerId, data.OrderId);

        var customer = await _customerService.GetCustomerAsync(data.CustomerId, cancellationToken);
        if (customer == null)
            throw new InvalidOperationException($"Customer {data.CustomerId} not found");

        if (!customer.IsActive)
            throw new InvalidOperationException($"Customer {data.CustomerId} is not active");

        data.CustomerValidated = true;
        _logger.LogInformation("Customer {CustomerId} validated successfully", data.CustomerId);
    }

    public override async Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Compensating customer validation for order {OrderId}", data.OrderId);
        data.CustomerValidated = false;
        await Task.CompletedTask;
    }
}

public class ReserveInventoryStep : SagaStep<OrderSagaData>
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<ReserveInventoryStep> _logger;

    public ReserveInventoryStep(IInventoryService inventoryService, ILogger<ReserveInventoryStep> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public override string Name => "ReserveInventory";
    public override string Description => "Reserves inventory for the order items";

    public override async Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reserving inventory for order {OrderId}", data.OrderId);

        foreach (var item in data.Items)
        {
            await _inventoryService.ReserveInventoryAsync(item.ProductId, item.Quantity, cancellationToken);
            _logger.LogInformation("Reserved {Quantity} units of product {ProductId}", 
                item.Quantity, item.ProductId);
        }

        data.InventoryReserved = true;
        _logger.LogInformation("Inventory reserved successfully for order {OrderId}", data.OrderId);
    }

    public override async Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Compensating inventory reservation for order {OrderId}", data.OrderId);

        foreach (var item in data.Items)
        {
            await _inventoryService.ReleaseInventoryAsync(item.ProductId, item.Quantity, cancellationToken);
            _logger.LogInformation("Released {Quantity} units of product {ProductId}", 
                item.Quantity, item.ProductId);
        }

        data.InventoryReserved = false;
        _logger.LogInformation("Inventory reservation compensated for order {OrderId}", data.OrderId);
    }
}

public class ProcessPaymentStep : SagaStep<OrderSagaData>
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<ProcessPaymentStep> _logger;

    public ProcessPaymentStep(IPaymentService paymentService, ILogger<ProcessPaymentStep> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    public override string Name => "ProcessPayment";
    public override string Description => "Processes payment for the order";

    public override async Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing payment for order {OrderId}", data.OrderId);

        var paymentResult = await _paymentService.ProcessPaymentAsync(new ProcessPaymentRequest
        {
            OrderId = data.OrderId,
            CustomerId = data.CustomerId,
            Amount = data.TotalAmount,
            Currency = data.Currency
        }, cancellationToken);

        if (!paymentResult.Success)
            throw new InvalidOperationException($"Payment failed: {paymentResult.ErrorMessage}");

        data.PaymentProcessed = true;
        data.PaymentId = paymentResult.PaymentId;
        _logger.LogInformation("Payment processed successfully for order {OrderId}, PaymentId: {PaymentId}", 
            data.OrderId, data.PaymentId);
    }

    public override async Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Compensating payment for order {OrderId}, PaymentId: {PaymentId}", 
            data.OrderId, data.PaymentId);

        if (data.PaymentId.HasValue)
        {
            await _paymentService.RefundPaymentAsync(data.PaymentId.Value, cancellationToken);
            _logger.LogInformation("Payment refunded successfully for order {OrderId}", data.OrderId);
        }

        data.PaymentProcessed = false;
        data.PaymentId = null;
    }
}

public class CreateOrderStep : SagaStep<OrderSagaData>
{
    private readonly IOrderService _orderService;
    private readonly ILogger<CreateOrderStep> _logger;

    public CreateOrderStep(IOrderService orderService, ILogger<CreateOrderStep> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public override string Name => "CreateOrder";
    public override string Description => "Creates the order in the system";

    public override async Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating order {OrderId}", data.OrderId);

        var order = await _orderService.CreateOrderAsync(new CreateOrderRequest
        {
            OrderId = data.OrderId,
            CustomerId = data.CustomerId,
            Items = data.Items,
            TotalAmount = data.TotalAmount,
            Currency = data.Currency
        }, cancellationToken);

        data.OrderCreated = true;
        _logger.LogInformation("Order {OrderId} created successfully", data.OrderId);
    }

    public override async Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Compensating order creation for order {OrderId}", data.OrderId);

        await _orderService.CancelOrderAsync(data.OrderId, cancellationToken);
        data.OrderCreated = false;
        _logger.LogInformation("Order {OrderId} cancelled successfully", data.OrderId);
    }
}

public class SendNotificationStep : SagaStep<OrderSagaData>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<SendNotificationStep> _logger;

    public SendNotificationStep(INotificationService notificationService, ILogger<SendNotificationStep> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public override string Name => "SendNotification";
    public override string Description => "Sends notification to the customer";

    public override async Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending notification for order {OrderId}", data.OrderId);

        await _notificationService.SendOrderConfirmationAsync(new OrderConfirmationNotification
        {
            OrderId = data.OrderId,
            CustomerId = data.CustomerId,
            TotalAmount = data.TotalAmount,
            Currency = data.Currency
        }, cancellationToken);

        data.NotificationSent = true;
        _logger.LogInformation("Notification sent successfully for order {OrderId}", data.OrderId);
    }

    public override async Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Compensating notification for order {OrderId}", data.OrderId);
        // Notifications typically don't need compensation
        data.NotificationSent = false;
        await Task.CompletedTask;
    }
}
```

## Saga State Management

### 1. Saga Store Interface

```csharp
public interface ISagaStore
{
    Task SaveSagaStateAsync<TData>(SagaState<TData> sagaState, CancellationToken cancellationToken = default)
        where TData : class;
    
    Task<SagaState<TData>?> GetSagaStateAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default)
        where TData : class;
    
    Task DeleteSagaStateAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default)
        where TData : class;
    
    Task<IEnumerable<SagaState<TData>>> GetSagaStatesAsync<TData>(
        SagaStatus? status = null, 
        CancellationToken cancellationToken = default) 
        where TData : class;
}
```

### 2. In-Memory Saga Store Implementation

```csharp
public class InMemorySagaStore : ISagaStore
{
    private readonly ConcurrentDictionary<Guid, object> _sagaStates = new();
    private readonly ILogger<InMemorySagaStore> _logger;

    public InMemorySagaStore(ILogger<InMemorySagaStore> logger)
    {
        _logger = logger;
    }

    public Task SaveSagaStateAsync<TData>(SagaState<TData> sagaState, CancellationToken cancellationToken = default)
        where TData : class
    {
        _sagaStates[sagaState.Id] = sagaState;
        _logger.LogDebug("Saved saga state for {SagaId}", sagaState.Id);
        return Task.CompletedTask;
    }

    public Task<SagaState<TData>?> GetSagaStateAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default)
        where TData : class
    {
        if (_sagaStates.TryGetValue(sagaId, out var state) && state is SagaState<TData> sagaState)
        {
            return Task.FromResult<SagaState<TData>?>(sagaState);
        }
        
        return Task.FromResult<SagaState<TData>?>(null);
    }

    public Task DeleteSagaStateAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default)
        where TData : class
    {
        _sagaStates.TryRemove(sagaId, out _);
        _logger.LogDebug("Deleted saga state for {SagaId}", sagaId);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<SagaState<TData>>> GetSagaStatesAsync<TData>(
        SagaStatus? status = null, 
        CancellationToken cancellationToken = default) 
        where TData : class
    {
        var states = _sagaStates.Values
            .OfType<SagaState<TData>>()
            .Where(s => status == null || s.Status == status)
            .ToList();

        return Task.FromResult<IEnumerable<SagaState<TData>>>(states);
    }
}
```

### 3. Database Saga Store Implementation

```csharp
public class DatabaseSagaStore : ISagaStore
{
    private readonly DbContext _context;
    private readonly ILogger<DatabaseSagaStore> _logger;

    public DatabaseSagaStore(DbContext context, ILogger<DatabaseSagaStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveSagaStateAsync<TData>(SagaState<TData> sagaState, CancellationToken cancellationToken = default)
        where TData : class
    {
        var existingState = await _context.Set<SagaState<TData>>()
            .FirstOrDefaultAsync(s => s.Id == sagaState.Id, cancellationToken);

        if (existingState != null)
        {
            _context.Entry(existingState).CurrentValues.SetValues(sagaState);
        }
        else
        {
            await _context.Set<SagaState<TData>>().AddAsync(sagaState, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Saved saga state for {SagaId}", sagaState.Id);
    }

    public async Task<SagaState<TData>?> GetSagaStateAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default)
        where TData : class
    {
        return await _context.Set<SagaState<TData>>()
            .FirstOrDefaultAsync(s => s.Id == sagaId, cancellationToken);
    }

    public async Task DeleteSagaStateAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default)
        where TData : class
    {
        var sagaState = await _context.Set<SagaState<TData>>()
            .FirstOrDefaultAsync(s => s.Id == sagaId, cancellationToken);

        if (sagaState != null)
        {
            _context.Set<SagaState<TData>>().Remove(sagaState);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Deleted saga state for {SagaId}", sagaId);
        }
    }

    public async Task<IEnumerable<SagaState<TData>>> GetSagaStatesAsync<TData>(
        SagaStatus? status = null, 
        CancellationToken cancellationToken = default) 
        where TData : class
    {
        var query = _context.Set<SagaState<TData>>().AsQueryable();
        
        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }
}
```

## Error Handling and Compensation

### 1. Saga Error Handling

```csharp
public class SagaErrorHandler
{
    private readonly ISagaOrchestrator _sagaOrchestrator;
    private readonly ILogger<SagaErrorHandler> _logger;

    public SagaErrorHandler(ISagaOrchestrator sagaOrchestrator, ILogger<SagaErrorHandler> logger)
    {
        _sagaOrchestrator = sagaOrchestrator;
        _logger = logger;
    }

    public async Task HandleSagaErrorAsync<TData>(Guid sagaId, Exception exception, CancellationToken cancellationToken = default)
        where TData : class
    {
        _logger.LogError(exception, "Handling error for saga {SagaId}", sagaId);

        var sagaState = await _sagaOrchestrator.GetSagaStateAsync<TData>(sagaId, cancellationToken);
        if (sagaState == null)
        {
            _logger.LogError("Saga {SagaId} not found for error handling", sagaId);
            return;
        }

        // Update saga state with error information
        sagaState.Status = SagaStatus.Failed;
        sagaState.Error = exception.Message;
        sagaState.UpdatedAt = DateTimeOffset.UtcNow;

        // Start compensation process
        await CompensateSagaAsync<TData>(sagaId, sagaState.CurrentStepIndex, cancellationToken);
    }

    private async Task CompensateSagaAsync<TData>(Guid sagaId, int currentStepIndex, CancellationToken cancellationToken = default)
        where TData : class
    {
        _logger.LogInformation("Starting compensation for saga {SagaId} from step {StepIndex}", 
            sagaId, currentStepIndex);

        // Compensate steps in reverse order
        for (int i = currentStepIndex - 1; i >= 0; i--)
        {
            try
            {
                await _sagaOrchestrator.CompensateStepAsync<TData>(sagaId, i, cancellationToken);
                _logger.LogInformation("Compensated step {StepIndex} for saga {SagaId}", i, sagaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compensate step {StepIndex} for saga {SagaId}", i, sagaId);
                // Continue with other steps even if one fails
            }
        }

        _logger.LogInformation("Compensation completed for saga {SagaId}", sagaId);
    }
}
```

### 2. Retry Logic

```csharp
public class SagaRetryHandler
{
    private readonly ISagaOrchestrator _sagaOrchestrator;
    private readonly ILogger<SagaRetryHandler> _logger;

    public SagaRetryHandler(ISagaOrchestrator sagaOrchestrator, ILogger<SagaRetryHandler> logger)
    {
        _sagaOrchestrator = sagaOrchestrator;
        _logger = logger;
    }

    public async Task<bool> ShouldRetryAsync<TData>(Guid sagaId, Exception exception, CancellationToken cancellationToken = default)
        where TData : class
    {
        var sagaState = await _sagaOrchestrator.GetSagaStateAsync<TData>(sagaId, cancellationToken);
        if (sagaState == null) return false;

        // Check if we've exceeded max retries
        if (sagaState.RetryCount >= sagaState.MaxRetries)
        {
            _logger.LogWarning("Saga {SagaId} has exceeded max retries ({MaxRetries})", 
                sagaId, sagaState.MaxRetries);
            return false;
        }

        // Check if the exception is retryable
        if (!IsRetryableException(exception))
        {
            _logger.LogWarning("Exception for saga {SagaId} is not retryable: {ExceptionType}", 
                sagaId, exception.GetType().Name);
            return false;
        }

        return true;
    }

    public async Task RetrySagaAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default)
        where TData : class
    {
        var sagaState = await _sagaOrchestrator.GetSagaStateAsync<TData>(sagaId, cancellationToken);
        if (sagaState == null) return;

        sagaState.RetryCount++;
        sagaState.Status = SagaStatus.Pending;
        sagaState.UpdatedAt = DateTimeOffset.UtcNow;

        _logger.LogInformation("Retrying saga {SagaId} (attempt {RetryCount}/{MaxRetries})", 
            sagaId, sagaState.RetryCount, sagaState.MaxRetries);

        // Wait before retry (exponential backoff)
        var delay = TimeSpan.FromSeconds(Math.Pow(2, sagaState.RetryCount));
        await Task.Delay(delay, cancellationToken);

        await _sagaOrchestrator.ExecuteNextStepAsync<TData>(sagaId, cancellationToken);
    }

    private bool IsRetryableException(Exception exception)
    {
        return exception is TimeoutException ||
               exception is HttpRequestException ||
               exception is TaskCanceledException ||
               (exception is InvalidOperationException && exception.Message.Contains("timeout"));
    }
}
```

## Complete Example: E-Commerce Order Processing

### 1. Order Saga Data

```csharp
public class OrderSagaData
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    
    // Step completion flags
    public bool CustomerValidated { get; set; }
    public bool InventoryReserved { get; set; }
    public bool PaymentProcessed { get; set; }
    public bool OrderCreated { get; set; }
    public bool NotificationSent { get; set; }
    
    // Step results
    public Guid? PaymentId { get; set; }
    public string? OrderNumber { get; set; }
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
```

### 2. Order Saga Implementation

```csharp
public class OrderSaga
{
    private readonly ISagaOrchestrator _sagaOrchestrator;
    private readonly ILogger<OrderSaga> _logger;

    public OrderSaga(ISagaOrchestrator sagaOrchestrator, ILogger<OrderSaga> logger)
    {
        _sagaOrchestrator = sagaOrchestrator;
        _logger = logger;
    }

    public async Task<Guid> StartOrderProcessingAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var sagaId = Guid.NewGuid();
        var sagaData = new OrderSagaData
        {
            OrderId = sagaId,
            CustomerId = request.CustomerId,
            Items = request.Items,
            TotalAmount = request.TotalAmount,
            Currency = request.Currency
        };

        _logger.LogInformation("Starting order processing saga {SagaId} for customer {CustomerId}", 
            sagaId, request.CustomerId);

        await _sagaOrchestrator.StartSagaAsync(sagaId, sagaData, cancellationToken);
        await _sagaOrchestrator.ExecuteNextStepAsync<OrderSagaData>(sagaId, cancellationToken);

        return sagaId;
    }

    public async Task<SagaState<OrderSagaData>?> GetOrderStatusAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        return await _sagaOrchestrator.GetSagaStateAsync<OrderSagaData>(sagaId, cancellationToken);
    }
}
```

### 3. Order Service Integration

```csharp
public class OrderService
{
    private readonly OrderSaga _orderSaga;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(OrderSaga orderSaga, IOrderRepository orderRepository, ILogger<OrderService> logger)
    {
        _orderSaga = orderSaga;
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<Guid> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

        // Start the order processing saga
        var sagaId = await _orderSaga.StartOrderProcessingAsync(request, cancellationToken);
        
        _logger.LogInformation("Order processing saga {SagaId} started", sagaId);
        return sagaId;
    }

    public async Task<OrderStatusDto> GetOrderStatusAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var sagaState = await _orderSaga.GetOrderStatusAsync(orderId, cancellationToken);
        if (sagaState == null)
            throw new InvalidOperationException($"Order {orderId} not found");

        return new OrderStatusDto
        {
            OrderId = orderId,
            Status = sagaState.Status.ToString(),
            CurrentStep = sagaState.CurrentStepIndex,
            CreatedAt = sagaState.CreatedAt,
            UpdatedAt = sagaState.UpdatedAt,
            Error = sagaState.Error
        };
    }
}

public class OrderStatusDto
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? Error { get; set; }
}
```

### 4. API Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(OrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateOrder(CreateOrderRequest request)
    {
        var orderId = await _orderService.CreateOrderAsync(request);
        return Ok(orderId);
    }

    [HttpGet("{id}/status")]
    public async Task<ActionResult<OrderStatusDto>> GetOrderStatus(Guid id)
    {
        var status = await _orderService.GetOrderStatusAsync(id);
        return Ok(status);
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

        // Add saga services
        builder.Services.AddScoped<ISagaStore, DatabaseSagaStore>();
        builder.Services.AddScoped<ISagaOrchestrator, SagaOrchestrator>();
        builder.Services.AddScoped<SagaErrorHandler>();
        builder.Services.AddScoped<SagaRetryHandler>();

        // Add saga steps
        builder.Services.AddScoped<ValidateCustomerStep>();
        builder.Services.AddScoped<ReserveInventoryStep>();
        builder.Services.AddScoped<ProcessPaymentStep>();
        builder.Services.AddScoped<CreateOrderStep>();
        builder.Services.AddScoped<SendNotificationStep>();

        // Add services
        builder.Services.AddScoped<OrderSaga>();
        builder.Services.AddScoped<OrderService>();

        var app = builder.Build();

        app.Run();
    }
}
```

## Best Practices

### 1. Saga Design
- Keep saga steps focused on a single responsibility
- Design steps to be idempotent
- Implement proper compensation logic
- Use meaningful step names and descriptions

### 2. Error Handling
- Implement comprehensive error handling
- Use retry logic for transient failures
- Log errors appropriately
- Implement proper compensation strategies

### 3. State Management
- Use persistent storage for saga state
- Implement proper state transitions
- Handle concurrent access to saga state
- Implement proper cleanup strategies

### 4. Performance Considerations
- Use asynchronous operations
- Implement proper timeout handling
- Monitor saga performance
- Implement proper resource cleanup

### 5. Testing
- Test saga steps in isolation
- Test compensation logic
- Test error scenarios
- Implement proper test data setup

This guide provides a comprehensive foundation for implementing distributed transactions with Raziee.SharedKernel using the Saga pattern, including all the necessary patterns and practices for building resilient and maintainable distributed systems.
