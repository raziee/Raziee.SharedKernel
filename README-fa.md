# Raziee.SharedKernel

[![Build Status](https://github.com/raziee/Raziee.SharedKernel/workflows/CI/badge.svg)](https://github.com/raziee/Raziee.SharedKernel/actions)
[![Coverage](https://codecov.io/gh/raziee/Raziee.SharedKernel/branch/main/graph/badge.svg)](https://codecov.io/gh/raziee/Raziee.SharedKernel)
[![NuGet Version](https://img.shields.io/nuget/v/Raziee.SharedKernel.svg)](https://www.nuget.org/packages/Raziee.SharedKernel)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

یک کتابخانه جامع و قدرتمند برای پیاده‌سازی Domain-Driven Design (DDD)، CQRS و الگوهای Multi-Tenancy در برنامه‌های .NET که اجزای اساسی و الگوهای استاندارد را برای تسهیل توسعه برنامه‌های پیچیده و مقیاس‌پذیر فراهم می‌کند.

## معرفی

Raziee.SharedKernel یک کتابخانه جامع و قدرتمند برای پیاده‌سازی Domain-Driven Design (DDD)، CQRS و الگوهای Multi-Tenancy در برنامه‌های .NET است. این کتابخانه اجزای اساسی و الگوهای استاندارد را برای تسهیل توسعه برنامه‌های پیچیده و مقیاس‌پذیر فراهم می‌کند.

## ویژگی‌های کلیدی

### 🏗️ اجزای DDD
- **کلاس‌های پایه Entity**: `Entity<TId>`، `AggregateRoot<TId>` و `AuditableEntity<TId>`
- **Value Objects**: کلاس پایه `ValueObject` با مقایسه پیشرفته برابری
- **Soft Delete**: پشتیبانی از حذف نرم با فیلترهای خودکار کوئری

### 🎯 Domain-Driven Design
- **Domain Events**: سیستم رویدادهای دامنه با `DomainEvent` و `IDomainEventDispatcher`
- **Domain Exceptions**: استثناهای خاص دامنه شامل `DomainException`، `DomainValidationException`، `EntityNotFoundException` و `ConcurrencyException`
- **Aggregate Root**: الگوی Aggregate Root با مدیریت خودکار رویدادهای دامنه

### 🗄️ الگوی Repository
- **Repository های Generic**: پیاده‌سازی `IRepository<T>` و `EfRepository<T>`
- **الگوی Specification**: کلاس `BaseSpecification<TEntity, TId>` برای کوئری‌های پیچیده
- **Unit of Work**: مدیریت تراکنش با `IUnitOfWork` و `UnitOfWork`

### ⚡ پشتیبانی CQRS
- **ادغام MediatR**: ادغام کامل با MediatR
- **Pipeline Behaviors**:
  - `ValidationBehavior`: اعتبارسنجی خودکار با FluentValidation
  - `TransactionBehavior`: مدیریت خودکار تراکنش
  - `LoggingBehavior`: لاگ‌گیری خودکار درخواست/پاسخ
  - `CachingBehavior`: پشتیبانی از کش پاسخ

### 🗃️ دسترسی به داده
- **DbContextBase**: کلاس پایه با ارسال خودکار رویدادهای دامنه
- **Model Builder Extensions**: افزونه‌های Entity Framework model builder
- **مدیریت تراکنش**: پشتیبانی کامل از مدیریت تراکنش

### 🏢 Multi-Tenancy
- **جداسازی داده Multi-Tenant**: پشتیبانی از جداسازی داده multi-tenant
- **Entity های آگاه از Tenant**: پشتیبانی از entity های آگاه از tenant

### 🔧 Dependency Injection
- **الگوی Fluent Builder**: الگوی `SharedKernelBuilder` برای پیکربندی آسان
- **Extension Methods**: متدهای افزونه برای ثبت سرویس

### 🏗️ پشتیبانی Modular Monolith
- **Module Abstractions**: رابط `IModule` و `ModuleRegistry` برای مدیریت ماژول
- **Integration Events**: `IIntegrationEvent` و `IntegrationEventDispatcher` برای ارتباط بین ماژول‌ها
- **InMemoryEventBus**: Event bus درون‌حافظه برای معماری modular monolith

### 🚀 انتزاعات Microservices
- **Messaging Abstractions**: رابط‌های `IMessageBus`، `IMessagePublisher`، `IMessageConsumer`
- **الگوی Outbox/Inbox**: `IOutboxStore` و `IInboxStore` برای پیام‌رسانی قابل اعتماد
- **الگوی Saga**: `ISagaOrchestrator` و `SagaStep<TData>` برای تراکنش‌های توزیع‌شده
- **Circuit Breaker**: `ICircuitBreaker` و `IRetryPolicy` برای ارتباط سرویس مقاوم

## نصب

```bash
dotnet add package Raziee.SharedKernel
```

## شروع سریع

### 1. پیکربندی سرویس‌ها

```csharp
using Raziee.SharedKernel.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// اضافه کردن SharedKernel با پیکربندی پیش‌فرض
builder.Services.AddSharedKernel();

// یا پیکربندی ویژگی‌های خاص
builder.Services.AddSharedKernel(sharedKernel =>
{
    sharedKernel
        .AddDomainEvents()
        .AddCQRS()
        .AddRepositories()
        .AddUnitOfWork()
        .AddMultiTenancy()
        .AddModules()
        .AddIntegrationEvents()
        .AddMessaging()
        .AddDistributedTransactions()
        .AddServiceCommunication()
        .AddCaching()
        .AddLogging();
});
```

### 2. ایجاد Entity های دامنه

```csharp
using Raziee.SharedKernel.Domain.Entities;
using Raziee.SharedKernel.Domain.Events;

public class User : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Email { get; private set; }

    public User(Guid id, string name, string email) : base(id)
    {
        Name = name;
        Email = email;
        
        // ایجاد رویداد دامنه
        AddDomainEvent(new UserCreatedEvent(Id, Name, Email));
    }
}

public class UserCreatedEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Name { get; }
    public string Email { get; }

    public UserCreatedEvent(Guid userId, string name, string email)
    {
        UserId = userId;
        Name = name;
        Email = email;
    }
}
```

### 3. ایجاد Value Objects

```csharp
using Raziee.SharedKernel.Domain.ValueObjects;

public class Email : ValueObject
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.Contains("@"))
            throw new ArgumentException("فرمت ایمیل نامعتبر", nameof(value));
        
        Value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

### 4. ایجاد Commands و Queries

```csharp
using Raziee.SharedKernel.CQRS;

public class CreateUserCommand : ICommand<CreateUserResult>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CreateUserResult
{
    public Guid UserId { get; set; }
}

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, CreateUserResult>
{
    private readonly IRepository<User, Guid> _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(IRepository<User, Guid> userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateUserResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User(Guid.NewGuid(), request.Name, request.Email);
        
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return new CreateUserResult { UserId = user.Id };
    }
}
```

## معماری

Raziee.SharedKernel از اصول Clean Architecture پیروی می‌کند و فراهم می‌کند:

- **لایه دامنه**: Entity ها، Value Objects، Domain Events و Domain Exceptions
- **لایه برنامه**: CQRS، Pipeline Behaviors و Application Services
- **لایه زیرساخت**: پیاده‌سازی Repository، دسترسی به داده و سرویس‌های خارجی
- **لایه ارائه**: Controller ها، API endpoints و کامپوننت‌های UI

## مشارکت

ما از مشارکت‌ها استقبال می‌کنیم! لطفاً راهنمای مشارکت ما را ببینید.

## مجوز

این پروژه تحت مجوز MIT مجوز دارد - فایل [LICENSE](LICENSE) را برای جزئیات ببینید.

## مستندات

### 📚 مستندات اصلی
- 📖 [راهنمای شروع کار](docs/getting-started-fa.md) - [English](docs/getting-started.md)
- 🏗️ [نمای کلی معماری](docs/architecture-fa.md) - [English](docs/architecture.md)

### 🎯 راهنمای الگوها
- 🎯 [راهنمای Domain-Driven Design](docs/guides/ddd-guide-fa.md) - [English](docs/guides/ddd-guide.md)
- ⚡ [راهنمای CQRS](docs/guides/cqrs-guide-fa.md) - [English](docs/guides/cqrs-guide.md)
- 🗄️ [راهنمای الگوی Repository](docs/guides/repository-pattern-guide-fa.md) - [English](docs/guides/repository-pattern-guide.md)
- 🏢 [راهنمای Multi-Tenancy](docs/guides/multitenancy-guide-fa.md) - [English](docs/guides/multitenancy-guide.md)

### 🏗️ راهنمای معماری
- 🏢 [راهنمای Modular Monolith](docs/guides/modular-monolith-guide-fa.md) - [English](docs/guides/modular-monolith-guide.md)
- 🚀 [راهنمای Microservices](docs/guides/microservices-guide-fa.md) - [English](docs/guides/microservices-guide.md)
- 🏗️ [راهنمای معماری Vertical Slice](docs/guides/vertical-slice-architecture-guide-fa.md) - [English](docs/guides/vertical-slice-architecture-guide.md)

### 📨 ارتباط و رویدادها
- 📨 [راهنمای سیستم رویداد](docs/guides/event-system-guide-fa.md) - [English](docs/guides/event-system-guide.md)
- 📨 [راهنمای الگوهای Messaging](docs/guides/messaging-patterns-guide-fa.md) - [English](docs/guides/messaging-patterns-guide.md)
- 🔧 [راهنمای ارتباط سرویس‌ها](docs/guides/service-communication-guide-fa.md) - [English](docs/guides/service-communication-guide.md)

### 🔄 الگوهای پیشرفته
- 🔄 [راهنمای Distributed Transactions](docs/guides/distributed-transactions-guide-fa.md) - [English](docs/guides/distributed-transactions-guide.md)
- 🗃️ [راهنمای Entity Framework Extensions](docs/guides/entity-framework-extensions-guide-fa.md) - [English](docs/guides/entity-framework-extensions-guide.md)

### 📖 تمام راهنماها
- 📚 [فهرست کامل راهنماها](docs/guides/README-fa.md) - [English](docs/guides/README.md)

## پشتیبانی

- 🐛 [ردیاب مسائل](https://github.com/raziee/Raziee.SharedKernel/issues)
- 💬 [بحث‌ها](https://github.com/raziee/Raziee.SharedKernel/discussions)

---

**Raziee.SharedKernel** - ساخت برنامه‌های .NET قوی، مقیاس‌پذیر و قابل نگهداری با اصول Domain-Driven Design.
