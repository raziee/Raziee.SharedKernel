# راهنمای Distributed Transactions

این راهنمای جامع نحوه استفاده از Raziee.SharedKernel برای پیاده‌سازی distributed transactions با استفاده از الگوی Saga در برنامه‌های .NET شما را نشان می‌دهد.

## فهرست مطالب

- [مقدمه](#مقدمه)
- [نمای کلی Distributed Transactions](#نمای-کلی-distributed-transactions)
- [الگوی Saga](#الگوی-saga)
- [Saga Orchestrator](#saga-orchestrator)
- [Saga Steps](#saga-steps)
- [مدیریت Saga State](#مدیریت-saga-state)
- [مدیریت خطا و جبران](#مدیریت-خطا-و-جبران)
- [مثال کامل: پردازش سفارش تجارت الکترونیک](#مثال-کامل-پردازش-سفارش-تجارت-الکترونیک)
- [بهترین شیوه‌ها](#بهترین-شیوه‌ها)

## مقدمه

Distributed transactions برای حفظ سازگاری داده در چندین سرویس در معماری‌های microservices ضروری هستند. الگوی Saga راهی برای مدیریت فرآیندهای کسب‌وکار طولانی‌مدت که چندین سرویس را در بر می‌گیرد فراهم می‌کند در حالی که eventual consistency را حفظ می‌کند.

## نمای کلی Distributed Transactions

### 1. مشکل

در معماری‌های microservices، تراکنش‌های ACID سنتی در مرزهای سرویس امکان‌پذیر نیستند. این منجر به چالش‌هایی در حفظ سازگاری داده می‌شود زمانی که عملیات چندین سرویس را در بر می‌گیرد.

### 2. راه‌حل: الگوی Saga

الگوی Saga راهی برای مدیریت distributed transactions فراهم می‌کند با:
- تقسیم عملیات پیچیده به مراحل کوچکتر و قابل مدیریت
- پیاده‌سازی منطق جبران برای سناریوهای rollback
- حفظ eventual consistency در سرویس‌ها
- فراهم کردن دید در وضعیت تراکنش

## الگوی Saga

### 1. اجزای Saga

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
    Pending,        // Saga در انتظار اجرا است
    Running,        // Saga در حال اجرا است
    Completed,      // Saga با موفقیت تکمیل شده است
    Compensating,   // Saga شکست خورده و در حال جبران است
    Compensated,    // Saga به طور کامل جبران شده است
    Failed,         // Saga شکست خورده و قابل جبران نیست
}
```

### 2. کلاس پایه Saga Step

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

### 1. تعریف رابط

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

### 2. پیاده‌سازی Saga Orchestrator

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
            _logger.LogWarning("Saga {SagaId} is not in a runnable state: {Status}", sagaId, sagaState.Status);
            return;
        }

        try
        {
            // به‌روزرسانی وضعیت به Running
            sagaState.Status = SagaStatus.Running;
            sagaState.UpdatedAt = DateTimeOffset.UtcNow;
            await _sagaStore.SaveSagaStateAsync(sagaState, cancellationToken);

            // اجرای مرحله بعدی
            var steps = GetSagaSteps<TData>();
            if (sagaState.CurrentStepIndex >= steps.Count)
            {
                // Saga تکمیل شده است
                sagaState.Status = SagaStatus.Completed;
                sagaState.UpdatedAt = DateTimeOffset.UtcNow;
                await _sagaStore.SaveSagaStateAsync(sagaState, cancellationToken);
                
                _logger.LogInformation("Saga {SagaId} completed successfully", sagaId);
                return;
            }

            var currentStep = steps[sagaState.CurrentStepIndex];
            _logger.LogInformation("Executing step {StepName} for saga {SagaId}", currentStep.Name, sagaId);

            if (currentStep.CanExecute(sagaState.Data))
            {
                await currentStep.ExecuteAsync(sagaState.Data, cancellationToken);
                
                // به‌روزرسانی شاخص مرحله
                sagaState.CurrentStepIndex++;
                sagaState.UpdatedAt = DateTimeOffset.UtcNow;
                await _sagaStore.SaveSagaStateAsync(sagaState, cancellationToken);
                
                _logger.LogInformation("Step {StepName} executed successfully for saga {SagaId}", currentStep.Name, sagaId);
            }
            else
            {
                _logger.LogWarning("Step {StepName} cannot be executed for saga {SagaId}", currentStep.Name, sagaId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing step for saga {SagaId}", sagaId);
            
            // به‌روزرسانی وضعیت به Failed
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
            // به‌روزرسانی وضعیت به Compensating
            sagaState.Status = SagaStatus.Compensating;
            sagaState.UpdatedAt = DateTimeOffset.UtcNow;
            await _sagaStore.SaveSagaStateAsync(sagaState, cancellationToken);

            var steps = GetSagaSteps<TData>();
            if (stepIndex < 0 || stepIndex >= steps.Count)
            {
                _logger.LogWarning("Invalid step index {StepIndex} for saga {SagaId}", stepIndex, sagaId);
                return;
            }

            var stepToCompensate = steps[stepIndex];
            _logger.LogInformation("Compensating step {StepName} for saga {SagaId}", stepToCompensate.Name, sagaId);

            if (stepToCompensate.CanCompensate(sagaState.Data))
            {
                await stepToCompensate.CompensateAsync(sagaState.Data, cancellationToken);
                _logger.LogInformation("Step {StepName} compensated successfully for saga {SagaId}", stepToCompensate.Name, sagaId);
            }
            else
            {
                _logger.LogWarning("Step {StepName} cannot be compensated for saga {SagaId}", stepToCompensate.Name, sagaId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compensating step {StepIndex} for saga {SagaId}", stepIndex, sagaId);
            
            // به‌روزرسانی وضعیت به Failed
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

    private List<SagaStep<TData>> GetSagaSteps<TData>() where TData : class
    {
        // این متد باید بر اساس نوع TData مراحل مناسب را برگرداند
        // این یک پیاده‌سازی ساده است
        return new List<SagaStep<TData>>();
    }
}
```

## Saga Steps

### 1. تعریف Saga Steps

```csharp
// Order Creation Step
public class CreateOrderStep : SagaStep<OrderSagaData>
{
    public override string Name => "CreateOrder";
    public override string Description => "Create a new order";

    public override async Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        // منطق ایجاد سفارش
        var order = new Order(Guid.NewGuid(), data.CustomerId);
        foreach (var item in data.Items)
        {
            order.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);
        }

        data.OrderId = order.Id;
        data.Order = order;
    }

    public override async Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        // منطق جبران - حذف سفارش
        if (data.OrderId != Guid.Empty)
        {
            // حذف سفارش از پایگاه داده
            // این یک مثال ساده است
        }
    }
}

// Inventory Reservation Step
public class ReserveInventoryStep : SagaStep<OrderSagaData>
{
    public override string Name => "ReserveInventory";
    public override string Description => "Reserve inventory for order items";

    public override async Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        // منطق رزرو موجودی
        foreach (var item in data.Items)
        {
            // رزرو موجودی برای هر آیتم
            // این یک مثال ساده است
        }
    }

    public override async Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        // منطق جبران - آزاد کردن موجودی رزرو شده
        foreach (var item in data.Items)
        {
            // آزاد کردن موجودی رزرو شده
            // این یک مثال ساده است
        }
    }
}

// Payment Processing Step
public class ProcessPaymentStep : SagaStep<OrderSagaData>
{
    public override string Name => "ProcessPayment";
    public override string Description => "Process payment for the order";

    public override async Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        // منطق پردازش پرداخت
        // این یک مثال ساده است
        data.PaymentId = Guid.NewGuid();
    }

    public override async Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        // منطق جبران - بازگرداندن پرداخت
        if (data.PaymentId != Guid.Empty)
        {
            // بازگرداندن پرداخت
            // این یک مثال ساده است
        }
    }
}

// Shipping Creation Step
public class CreateShippingStep : SagaStep<OrderSagaData>
{
    public override string Name => "CreateShipping";
    public override string Description => "Create shipping for the order";

    public override async Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        // منطق ایجاد ارسال
        // این یک مثال ساده است
        data.ShippingId = Guid.NewGuid();
    }

    public override async Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        // منطق جبران - لغو ارسال
        if (data.ShippingId != Guid.Empty)
        {
            // لغو ارسال
            // این یک مثال ساده است
        }
    }
}
```

### 2. Saga Data Model

```csharp
public class OrderSagaData
{
    public Guid CustomerId { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid PaymentId { get; set; }
    public Guid ShippingId { get; set; }
    public decimal TotalAmount { get; set; }
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
```

## مدیریت Saga State

### 1. Saga Store Interface

```csharp
public interface ISagaStore
{
    Task SaveSagaStateAsync<TData>(SagaState<TData> sagaState, CancellationToken cancellationToken = default)
        where TData : class;
    
    Task<SagaState<TData>?> GetSagaStateAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default)
        where TData : class;
    
    Task<IEnumerable<SagaState<TData>>> GetSagaStatesByStatusAsync<TData>(SagaStatus status, CancellationToken cancellationToken = default)
        where TData : class;
    
    Task DeleteSagaStateAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default)
        where TData : class;
}
```

### 2. پیاده‌سازی Saga Store

```csharp
public class EfSagaStore : ISagaStore
{
    private readonly DbContext _context;
    private readonly ILogger<EfSagaStore> _logger;

    public EfSagaStore(DbContext context, ILogger<EfSagaStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveSagaStateAsync<TData>(SagaState<TData> sagaState, CancellationToken cancellationToken = default)
        where TData : class
    {
        _logger.LogDebug("Saving saga state {SagaId}", sagaState.Id);

        var existingState = await _context.Set<SagaState<TData>>()
            .FirstOrDefaultAsync(s => s.Id == sagaState.Id, cancellationToken);

        if (existingState != null)
        {
            // به‌روزرسانی موجود
            existingState.CurrentStepIndex = sagaState.CurrentStepIndex;
            existingState.Data = sagaState.Data;
            existingState.Status = sagaState.Status;
            existingState.UpdatedAt = sagaState.UpdatedAt;
            existingState.Error = sagaState.Error;
            existingState.RetryCount = sagaState.RetryCount;
        }
        else
        {
            // ایجاد جدید
            _context.Set<SagaState<TData>>().Add(sagaState);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Saga state {SagaId} saved successfully", sagaState.Id);
    }

    public async Task<SagaState<TData>?> GetSagaStateAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default)
        where TData : class
    {
        _logger.LogDebug("Getting saga state {SagaId}", sagaId);

        var sagaState = await _context.Set<SagaState<TData>>()
            .FirstOrDefaultAsync(s => s.Id == sagaId, cancellationToken);

        if (sagaState != null)
        {
            _logger.LogDebug("Saga state {SagaId} found with status {Status}", sagaId, sagaState.Status);
        }
        else
        {
            _logger.LogDebug("Saga state {SagaId} not found", sagaId);
        }

        return sagaState;
    }

    public async Task<IEnumerable<SagaState<TData>>> GetSagaStatesByStatusAsync<TData>(SagaStatus status, CancellationToken cancellationToken = default)
        where TData : class
    {
        _logger.LogDebug("Getting saga states with status {Status}", status);

        var sagaStates = await _context.Set<SagaState<TData>>()
            .Where(s => s.Status == status)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Found {Count} saga states with status {Status}", sagaStates.Count, status);
        return sagaStates;
    }

    public async Task DeleteSagaStateAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default)
        where TData : class
    {
        _logger.LogDebug("Deleting saga state {SagaId}", sagaId);

        var sagaState = await _context.Set<SagaState<TData>>()
            .FirstOrDefaultAsync(s => s.Id == sagaId, cancellationToken);

        if (sagaState != null)
        {
            _context.Set<SagaState<TData>>().Remove(sagaState);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Saga state {SagaId} deleted successfully", sagaId);
        }
        else
        {
            _logger.LogWarning("Saga state {SagaId} not found for deletion", sagaId);
        }
    }
}
```

## مدیریت خطا و جبران

### 1. Error Handling Strategy

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

        // افزایش تعداد retry
        sagaState.RetryCount++;
        sagaState.Error = exception.Message;

        if (sagaState.RetryCount < sagaState.MaxRetries)
        {
            _logger.LogInformation("Retrying saga {SagaId} (attempt {RetryCount}/{MaxRetries})", 
                sagaId, sagaState.RetryCount, sagaState.MaxRetries);
            
            // retry بعد از delay
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, sagaState.RetryCount)), cancellationToken);
            await _sagaOrchestrator.ExecuteNextStepAsync<TData>(sagaId, cancellationToken);
        }
        else
        {
            _logger.LogError("Saga {SagaId} failed after {MaxRetries} retries", sagaId, sagaState.MaxRetries);
            
            // شروع فرآیند جبران
            await StartCompensationAsync<TData>(sagaId, cancellationToken);
        }
    }

    private async Task StartCompensationAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default)
        where TData : class
    {
        _logger.LogInformation("Starting compensation for saga {SagaId}", sagaId);

        var sagaState = await _sagaOrchestrator.GetSagaStateAsync<TData>(sagaId, cancellationToken);
        if (sagaState == null)
        {
            _logger.LogError("Saga {SagaId} not found for compensation", sagaId);
            return;
        }

        // جبران مراحل به ترتیب معکوس
        for (int i = sagaState.CurrentStepIndex - 1; i >= 0; i--)
        {
            try
            {
                await _sagaOrchestrator.CompensateStepAsync<TData>(sagaId, i, cancellationToken);
                _logger.LogInformation("Compensated step {StepIndex} for saga {SagaId}", i, sagaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compensating step {StepIndex} for saga {SagaId}", i, sagaId);
                // ادامه جبران حتی در صورت خطا
            }
        }

        _logger.LogInformation("Compensation completed for saga {SagaId}", sagaId);
    }
}
```

### 2. Retry Policy

```csharp
public class SagaRetryPolicy
{
    private readonly ILogger<SagaRetryPolicy> _logger;

    public SagaRetryPolicy(ILogger<SagaRetryPolicy> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ShouldRetryAsync<TData>(SagaState<TData> sagaState, Exception exception, CancellationToken cancellationToken = default)
        where TData : class
    {
        if (sagaState.RetryCount >= sagaState.MaxRetries)
        {
            _logger.LogWarning("Saga {SagaId} has exceeded maximum retries", sagaState.Id);
            return false;
        }

        // بررسی نوع خطا برای تصمیم‌گیری retry
        if (IsRetryableError(exception))
        {
            _logger.LogInformation("Error is retryable for saga {SagaId}", sagaState.Id);
            return true;
        }

        _logger.LogWarning("Error is not retryable for saga {SagaId}: {Error}", sagaState.Id, exception.Message);
        return false;
    }

    public TimeSpan CalculateRetryDelay(int retryCount)
    {
        // Exponential backoff
        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
        return TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds, 300)); // حداکثر 5 دقیقه
    }

    private bool IsRetryableError(Exception exception)
    {
        return exception switch
        {
            TimeoutException => true,
            HttpRequestException => true,
            TaskCanceledException => true,
            InvalidOperationException => false,
            ArgumentException => false,
            _ => true // پیش‌فرض retry
        };
    }
}
```

## مثال کامل: پردازش سفارش تجارت الکترونیک

### 1. Order Saga Implementation

```csharp
public class OrderSaga
{
    private readonly ISagaOrchestrator _sagaOrchestrator;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPaymentService _paymentService;
    private readonly IShippingService _shippingService;
    private readonly ILogger<OrderSaga> _logger;

    public OrderSaga(
        ISagaOrchestrator sagaOrchestrator,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IPaymentService paymentService,
        IShippingService shippingService,
        ILogger<OrderSaga> logger)
    {
        _sagaOrchestrator = sagaOrchestrator;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _paymentService = paymentService;
        _shippingService = shippingService;
        _logger = logger;
    }

    public async Task<Guid> StartOrderSagaAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting order saga for customer {CustomerId}", request.CustomerId);

        var sagaId = Guid.NewGuid();
        var sagaData = new OrderSagaData
        {
            CustomerId = request.CustomerId,
            Items = request.Items,
            TotalAmount = request.Items.Sum(i => i.UnitPrice * i.Quantity)
        };

        await _sagaOrchestrator.StartSagaAsync(sagaId, sagaData, cancellationToken);
        
        // اجرای اولین مرحله
        await _sagaOrchestrator.ExecuteNextStepAsync<OrderSagaData>(sagaId, cancellationToken);

        _logger.LogInformation("Order saga {SagaId} started successfully", sagaId);
        return sagaId;
    }

    public async Task<SagaState<OrderSagaData>?> GetSagaStatusAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        return await _sagaOrchestrator.GetSagaStateAsync<OrderSagaData>(sagaId, cancellationToken);
    }
}
```

### 2. Order Saga Steps Implementation

```csharp
public class CreateOrderStep : SagaStep<OrderSagaData>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<CreateOrderStep> _logger;

    public CreateOrderStep(IOrderRepository orderRepository, ILogger<CreateOrderStep> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public override string Name => "CreateOrder";
    public override string Description => "Create a new order";

    public override async Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", data.CustomerId);

        var order = new Order(Guid.NewGuid(), data.CustomerId);
        foreach (var item in data.Items)
        {
            order.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);
        }

        await _orderRepository.AddAsync(order, cancellationToken);
        data.OrderId = order.Id;
        data.Order = order;

        _logger.LogInformation("Order {OrderId} created successfully", order.Id);
    }

    public override async Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Compensating order creation for order {OrderId}", data.OrderId);

        if (data.OrderId != Guid.Empty)
        {
            await _orderRepository.DeleteByIdAsync(data.OrderId, cancellationToken);
            _logger.LogInformation("Order {OrderId} deleted successfully", data.OrderId);
        }
    }
}

public class ReserveInventoryStep : SagaStep<OrderSagaData>
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ReserveInventoryStep> _logger;

    public ReserveInventoryStep(IProductRepository productRepository, ILogger<ReserveInventoryStep> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public override string Name => "ReserveInventory";
    public override string Description => "Reserve inventory for order items";

    public override async Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reserving inventory for order {OrderId}", data.OrderId);

        foreach (var item in data.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            if (product == null)
                throw new InvalidOperationException($"Product {item.ProductId} not found");

            if (product.StockQuantity < item.Quantity)
                throw new InvalidOperationException($"Insufficient stock for product {item.ProductId}");

            // رزرو موجودی
            product.ReserveStock(item.Quantity);
            await _productRepository.UpdateAsync(product, cancellationToken);
        }

        _logger.LogInformation("Inventory reserved successfully for order {OrderId}", data.OrderId);
    }

    public override async Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Compensating inventory reservation for order {OrderId}", data.OrderId);

        foreach (var item in data.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            if (product != null)
            {
                // آزاد کردن موجودی رزرو شده
                product.ReleaseStock(item.Quantity);
                await _productRepository.UpdateAsync(product, cancellationToken);
            }
        }

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
    public override string Description => "Process payment for the order";

    public override async Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing payment for order {OrderId}", data.OrderId);

        var paymentRequest = new ProcessPaymentRequest
        {
            OrderId = data.OrderId,
            Amount = data.TotalAmount,
            CustomerId = data.CustomerId
        };

        var paymentResult = await _paymentService.ProcessPaymentAsync(paymentRequest, cancellationToken);
        if (!paymentResult.Success)
            throw new InvalidOperationException($"Payment processing failed: {paymentResult.Error}");

        data.PaymentId = paymentResult.TransactionId;
        _logger.LogInformation("Payment processed successfully for order {OrderId}", data.OrderId);
    }

    public override async Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Compensating payment for order {OrderId}", data.OrderId);

        if (data.PaymentId != Guid.Empty)
        {
            await _paymentService.RefundPaymentAsync(data.PaymentId, cancellationToken);
            _logger.LogInformation("Payment refunded successfully for order {OrderId}", data.OrderId);
        }
    }
}

public class CreateShippingStep : SagaStep<OrderSagaData>
{
    private readonly IShippingService _shippingService;
    private readonly ILogger<CreateShippingStep> _logger;

    public CreateShippingStep(IShippingService shippingService, ILogger<CreateShippingStep> logger)
    {
        _shippingService = shippingService;
        _logger = logger;
    }

    public override string Name => "CreateShipping";
    public override string Description => "Create shipping for the order";

    public override async Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating shipping for order {OrderId}", data.OrderId);

        var shippingRequest = new CreateShipmentRequest
        {
            OrderId = data.OrderId,
            CustomerId = data.CustomerId,
            Items = data.Items
        };

        var shippingResult = await _shippingService.CreateShipmentAsync(shippingRequest, cancellationToken);
        if (!shippingResult.Success)
            throw new InvalidOperationException($"Shipping creation failed: {shippingResult.Error}");

        data.ShippingId = shippingResult.ShipmentId;
        _logger.LogInformation("Shipping created successfully for order {OrderId}", data.OrderId);
    }

    public override async Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Compensating shipping for order {OrderId}", data.OrderId);

        if (data.ShippingId != Guid.Empty)
        {
            await _shippingService.CancelShipmentAsync(data.ShippingId, cancellationToken);
            _logger.LogInformation("Shipping cancelled successfully for order {OrderId}", data.OrderId);
        }
    }
}
```

### 3. API Controller

```csharp
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderSaga _orderSaga;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(OrderSaga orderSaga, ILogger<OrdersController> logger)
    {
        _orderSaga = orderSaga;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<CreateOrderResponse>> CreateOrder(CreateOrderRequest request)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

        try
        {
            var sagaId = await _orderSaga.StartOrderSagaAsync(request);
            return Ok(new CreateOrderResponse { SagaId = sagaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for customer {CustomerId}", request.CustomerId);
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{sagaId}/status")]
    public async Task<ActionResult<SagaStatusResponse>> GetSagaStatus(Guid sagaId)
    {
        var sagaState = await _orderSaga.GetSagaStatusAsync(sagaId);
        if (sagaState == null)
            return NotFound();

        return Ok(new SagaStatusResponse
        {
            SagaId = sagaState.Id,
            Status = sagaState.Status.ToString(),
            CurrentStepIndex = sagaState.CurrentStepIndex,
            Error = sagaState.Error,
            CreatedAt = sagaState.CreatedAt,
            UpdatedAt = sagaState.UpdatedAt
        });
    }
}
```

## بهترین شیوه‌ها

### 1. طراحی Saga
- Saga ها را بر اساس فرآیندهای کسب‌وکار طراحی کنید
- از مراحل کوچک و قابل مدیریت استفاده کنید
- منطق جبران مناسب پیاده‌سازی کنید
- از idempotency استفاده کنید

### 2. مدیریت خطا
- retry policies را پیاده‌سازی کنید
- error handling مناسب پیاده‌سازی کنید
- dead letter queues را استفاده کنید
- monitoring و alerting را تنظیم کنید

### 3. عملکرد
- async processing را پیاده‌سازی کنید
- batch processing را استفاده کنید
- caching مناسب استفاده کنید
- resource management را در نظر بگیرید

### 4. امنیت
- authentication و authorization را پیاده‌سازی کنید
- data encryption را استفاده کنید
- audit logging را پیاده‌سازی کنید
- access control را تنظیم کنید

### 5. تست
- unit tests را بنویسید
- integration tests را پیاده‌سازی کنید
- end-to-end tests را انجام دهید
- performance tests را اجرا کنید

این راهنما پایه جامعی برای پیاده‌سازی distributed transactions با Raziee.SharedKernel ارائه می‌دهد، شامل تمام الگوها و شیوه‌های لازم برای ساخت سیستم‌های توزیع‌شده قابل اعتماد و مقیاس‌پذیر.
