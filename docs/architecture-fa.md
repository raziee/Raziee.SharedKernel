# راهنمای معماری

این سند نمای کلی از معماری و تصمیمات طراحی پشت Raziee.SharedKernel را ارائه می‌دهد.

## فهرست مطالب

- [نمای کلی معماری](#نمای-کلی-معماری)
- [اصول طراحی](#اصول-طراحی)
- [کامپوننت‌های اصلی](#کامپوننت‌های-اصلی)
- [الگوها و شیوه‌ها](#الگوها-و-شیوه‌ها)
- [معماری لایه‌ای](#معماری-لایه‌ای)
- [پشتیبانی از Modular Monolith](#پشتیبانی-از-modular-monolith)
- [انتزاعات میکروسرویس](#انتزاعات-میکروسرویس)
- [ملاحظات عملکرد](#ملاحظات-عملکرد)
- [ملاحظات امنیتی](#ملاحظات-امنیتی)

## نمای کلی معماری

Raziee.SharedKernel از اصول Clean Architecture پیروی می‌کند و پایه‌ای جامع برای ساخت برنامه‌های مبتنی بر دامنه فراهم می‌کند. این کتابخانه به گونه‌ای طراحی شده که:

- **ماژولار**: کامپوننت‌ها می‌توانند مستقل استفاده شوند
- **قابل توسعه**: آسان برای توسعه و سفارشی‌سازی
- **قابل تست**: با در نظر گیری قابلیت تست ساخته شده
- **عملکرد بالا**: بهینه‌سازی شده برای عملکرد
- **قابل نگهداری**: کد تمیز، خوانا و مستند

## اصول طراحی

### 1. Domain-Driven Design (DDD)

کتابخانه بر اساس اصول DDD ساخته شده:

- **Entities**: نمایانگر اشیاء با هویت
- **Value Objects**: نمایانگر مفاهیم بدون هویت
- **Aggregates**: مرزهای سازگاری
- **Domain Events**: ارتباط بین aggregates
- **Repositories**: انتزاع دسترسی به داده

### 2. جداسازی نگرانی‌ها

هر کامپوننت یک مسئولیت واحد دارد:

- **لایه دامنه**: منطق و قوانین کسب‌وکار
- **لایه برنامه**: موارد استفاده و هماهنگی
- **لایه زیرساخت**: نگرانی‌های خارجی
- **لایه ارائه**: رابط کاربری

### 3. وارونگی وابستگی

ماژول‌های سطح بالا به ماژول‌های سطح پایین وابسته نیستند. هر دو به انتزاعات وابسته‌اند.

### 4. اصل باز/بسته

کتابخانه برای توسعه باز است اما برای تغییر بسته است.

## کامپوننت‌های اصلی

### بلوک‌های سازنده دامنه

#### Entities

```csharp
public abstract class Entity<TId> : IEquatable<Entity<TId>>
{
    public TId Id { get; protected set; }
    // برابری بر اساس ID
}

public abstract class AggregateRoot<TId> : Entity<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void AddDomainEvent(IDomainEvent domainEvent);
    public void ClearDomainEvents();
}
```

#### Value Objects

```csharp
public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object> GetEqualityComponents();
    // برابری ساختاری بر اساس کامپوننت‌ها
}
```

#### Domain Events

```csharp
public interface IDomainEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredOn { get; }
    int Version { get; }
}
```

### الگوی Repository

#### Repository عمومی

```csharp
public interface IRepository<TEntity, TId> : IReadRepository<TEntity, TId>
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
```

#### الگوی Specification

```csharp
public abstract class BaseSpecification<TEntity, TId> : ISpecification<TEntity>
{
    public Expression<Func<TEntity, bool>>? Criteria { get; private set; }
    public List<Expression<Func<TEntity, object>>> Includes { get; } = new();
    public Expression<Func<TEntity, object>>? OrderBy { get; private set; }
    // ... سایر ویژگی‌ها
}
```

### پشتیبانی از CQRS

#### Commands و Queries

```csharp
public interface ICommand : IRequest { }
public interface ICommand<TResponse> : IRequest<TResponse> { }
public interface IQuery<TResponse> : IRequest<TResponse> { }
```

#### Pipeline Behaviors

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    // اعتبارسنجی خودکار با FluentValidation
}

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    // مدیریت خودکار تراکنش
}
```

## الگوها و شیوه‌ها

### 1. الگوی Unit of Work

سازگاری داده در عملیات متعدد را تضمین می‌کند:

```csharp
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

### 2. الگوی Domain Events

ارتباط سست بین aggregates را امکان‌پذیر می‌کند:

```csharp
public class DomainEventDispatcher : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        // ارسال events به handlers ثبت شده
    }
}
```

### 3. الگوی Specification

قوانین کسب‌وکار برای دسترسی به داده را کپسوله می‌کند:

```csharp
public class UserByEmailSpecification : BaseSpecification<User, Guid>
{
    public UserByEmailSpecification(string email)
    {
        AddCriteria(u => u.Email == email);
    }
}
```

### 4. الگوی Factory

برای ایجاد اشیاء پیچیده:

```csharp
public class UserFactory
{
    public User CreateUser(string name, string email)
    {
        return new User(Guid.NewGuid(), name, email);
    }
}
```

## معماری لایه‌ای

### لایه دامنه

شامل منطق و قوانین کسب‌وکار:

- **Entities**: `User`, `Product`, `Order`
- **Value Objects**: `Email`, `Address`, `Money`
- **Domain Events**: `UserCreatedEvent`, `OrderPlacedEvent`
- **Domain Services**: `UserDomainService`, `OrderDomainService`

### لایه برنامه

شامل موارد استفاده و هماهنگی:

- **Commands**: `CreateUserCommand`, `UpdateUserCommand`
- **Queries**: `GetUserByIdQuery`, `GetUsersQuery`
- **Handlers**: `CreateUserCommandHandler`, `GetUserByIdQueryHandler`
- **Application Services**: `UserApplicationService`

### لایه زیرساخت

شامل نگرانی‌های خارجی:

- **Repositories**: `EfUserRepository`, `MongoUserRepository`
- **External Services**: `EmailService`, `PaymentService`
- **Data Access**: `DbContext`, `MongoContext`

### لایه ارائه

شامل رابط کاربری:

- **Controllers**: `UsersController`, `OrdersController`
- **ViewModels**: `UserViewModel`, `OrderViewModel`
- **API Endpoints**: RESTful APIs, GraphQL

## پشتیبانی از Modular Monolith

### انتزاعات ماژول

```csharp
public interface IModule
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    IEnumerable<string> Dependencies { get; }
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}
```

### Integration Events

```csharp
public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredOn { get; }
    string SourceModule { get; }
}
```

### ارتباط ماژول

```csharp
public interface IModuleCommunication
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
    Task SubscribeAsync<TEvent>(Func<TEvent, CancellationToken, Task> handler, CancellationToken cancellationToken = default);
    Task SendAsync<TMessage>(string targetModule, TMessage message, CancellationToken cancellationToken = default);
}
```

## انتزاعات میکروسرویس

### انتزاعات پیام‌رسانی

```csharp
public interface IMessageBus
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default);
    Task SubscribeAsync<TMessage>(Func<TMessage, CancellationToken, Task> handler, CancellationToken cancellationToken = default);
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
```

### الگوی Outbox/Inbox

```csharp
public interface IOutboxStore
{
    Task StoreAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(int batchSize = 100, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
}
```

### الگوی Saga

```csharp
public interface ISagaOrchestrator
{
    Task StartSagaAsync<TData>(Guid sagaId, TData data, CancellationToken cancellationToken = default);
    Task ExecuteNextStepAsync<TData>(Guid sagaId, CancellationToken cancellationToken = default);
    Task CompensateStepAsync<TData>(Guid sagaId, int stepIndex, CancellationToken cancellationToken = default);
}
```

### الگوی Circuit Breaker

```csharp
public interface ICircuitBreaker
{
    string Name { get; }
    CircuitBreakerState State { get; }
    Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default);
}
```

## ملاحظات عملکرد

### 1. Lazy Loading

از lazy loading برای entities مرتبط استفاده کنید:

```csharp
public class User : AggregateRoot<Guid>
{
    private readonly Lazy<List<Order>> _orders;
    
    public User(Guid id, string name, string email) : base(id)
    {
        Name = name;
        Email = email;
        _orders = new Lazy<List<Order>>(() => LoadOrders());
    }
    
    public IReadOnlyList<Order> Orders => _orders.Value;
}
```

### 2. Caching

کش برای داده‌های پر دسترس پیاده‌سازی کنید:

```csharp
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IMemoryCache _cache;
    
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var cacheKey = GenerateCacheKey(request);
        
        if (_cache.TryGetValue(cacheKey, out TResponse? cachedResponse))
            return cachedResponse!;
        
        var response = await next();
        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));
        
        return response;
    }
}
```

### 3. Async/Await

از async/await برای عملیات I/O استفاده کنید:

```csharp
public async Task<User> GetUserAsync(Guid userId)
{
    return await _userRepository.GetByIdAsync(userId);
}
```

### 4. Pagination

صفحه‌بندی برای مجموعه داده‌های بزرگ پیاده‌سازی کنید:

```csharp
public async Task<PaginatedResult<User>> GetUsersAsync(int pageNumber, int pageSize)
{
    return await _userRepository.GetPagedAsync(pageNumber, pageSize);
}
```

## ملاحظات امنیتی

### 1. اعتبارسنجی ورودی

تمام ورودی‌ها را اعتبارسنجی کنید:

```csharp
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
        
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
```

### 2. مجوزدهی

مجوزدهی مناسب پیاده‌سازی کنید:

```csharp
[Authorize]
public class UsersController : ControllerBase
{
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteUser(Guid id)
    {
        // منطق حذف کاربر
    }
}
```

### 3. حفاظت از داده

از داده‌های حساس محافظت کنید:

```csharp
public class User : AggregateRoot<Guid>
{
    private string _passwordHash;
    
    public string PasswordHash
    {
        get => _passwordHash;
        private set => _passwordHash = HashPassword(value);
    }
}
```

### 4. Audit Logging

عملیات مهم را لاگ کنید:

```csharp
public class AuditLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing {RequestType}", typeof(TRequest).Name);
        
        var response = await next();
        
        _logger.LogInformation("Completed {RequestType}", typeof(TRequest).Name);
        
        return response;
    }
}
```

## بهترین شیوه‌ها

### 1. مدل‌سازی دامنه

- aggregates را کوچک و متمرکز نگه دارید
- از value objects برای مفاهیم بدون هویت استفاده کنید
- domain events را برای رویدادهای مهم کسب‌وکار ایجاد کنید
- منطق کسب‌وکار را در لایه دامنه نگه دارید

### 2. الگوی Repository

- از repositories عمومی برای عملیات رایج استفاده کنید
- specifications را برای کوئری‌های پیچیده پیاده‌سازی کنید
- از unit of work برای مدیریت تراکنش استفاده کنید
- repositories را روی دسترسی به داده متمرکز نگه دارید

### 3. CQRS

- commands را از queries جدا کنید
- از handlers برای منطق کسب‌وکار استفاده کنید
- pipeline behaviors را برای نگرانی‌های متقاطع پیاده‌سازی کنید
- handlers را متمرکز و قابل تست نگه دارید

### 4. تست

- تست‌های واحد برای منطق دامنه بنویسید
- تست‌های یکپارچگی برای دسترسی به داده بنویسید
- از test doubles برای وابستگی‌های خارجی استفاده کنید
- پوشش تست بالا را حفظ کنید

### 5. مستندسازی

- API های عمومی را مستند کنید
- نمونه‌های کد ارائه دهید
- مستندات را به‌روز نگه دارید
- از نام‌های معنادار و نظرات استفاده کنید

## نتیجه‌گیری

Raziee.SharedKernel پایه‌ای محکم برای ساخت برنامه‌های مبتنی بر دامنه فراهم می‌کند. با پیروی از الگوها و شیوه‌های ذکر شده در این راهنما، می‌توانید برنامه‌های قابل نگهداری، قابل تست و مقیاس‌پذیر ایجاد کنید.

برای اطلاعات بیشتر، ببینید:

- [راهنمای شروع](getting-started-fa.md)
- [مرجع API](api-reference.md)
- [نمونه‌ها](examples/)
- [مخزن GitHub](https://github.com/raziee/Raziee.SharedKernel)
