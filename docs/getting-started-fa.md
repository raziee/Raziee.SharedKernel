# شروع با Raziee.SharedKernel

این راهنما به شما کمک می‌کند تا با Raziee.SharedKernel، یک کتابخانه پایه جامع DDD برای برنامه‌های .NET شروع کنید.

## فهرست مطالب

- [نصب](#نصب)
- [راه‌اندازی اولیه](#راه‌اندازی-اولیه)
- [ایجاد اولین Entity](#ایجاد-اولین-entity)
- [ایجاد Value Objects](#ایجاد-value-objects)
- [راه‌اندازی CQRS](#راه‌اندازی-cqrs)
- [الگوی Repository](#الگوی-repository)
- [Domain Events](#domain-events)
- [مراحل بعدی](#مراحل-بعدی)

## نصب

### پیش‌نیازها

- .NET 8.0 SDK یا بالاتر
- Visual Studio 2022 یا VS Code
- Entity Framework Core (برای دسترسی به داده)

### نصب پکیج

```bash
dotnet add package Raziee.SharedKernel
```

یا از طریق Package Manager Console:

```powershell
Install-Package Raziee.SharedKernel
```

## راه‌اندازی اولیه

### 1. پیکربندی سرویس‌ها

در `Program.cs` یا `Startup.cs`:

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

var app = builder.Build();
```

### 2. پیکربندی Entity Framework

```csharp
builder.Services.AddDbContext<YourDbContext>(options =>
    options.UseSqlServer(connectionString));

// پیکربندی SharedKernel DbContext
public class YourDbContext : DbContextBase
{
    public YourDbContext(DbContextOptions<YourDbContext> options, 
        IDomainEventDispatcher domainEventDispatcher, 
        ILogger<YourDbContext> logger) 
        : base(options, domainEventDispatcher, logger)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // پیکربندی entities شما
        modelBuilder.ConfigureAuditableEntities();
        modelBuilder.ConfigureSoftDelete();
        modelBuilder.ConfigureConcurrencyTokens();
    }
}
```

## ایجاد اولین Entity

### 1. ایجاد Domain Entity

```csharp
using Raziee.SharedKernel.Domain.Entities;
using Raziee.SharedKernel.Domain.Events;

public class User : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public User(Guid id, string name, string email) : base(id)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        CreatedAt = DateTime.UtcNow;
        
        // ایجاد domain event
        AddDomainEvent(new UserCreatedEvent(Id, Name, Email));
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrEmpty(newName))
            throw new ArgumentException("Name cannot be null or empty", nameof(newName));
        
        Name = newName;
        AddDomainEvent(new UserNameUpdatedEvent(Id, newName));
    }
}
```

### 2. ایجاد Domain Events

```csharp
using Raziee.SharedKernel.Domain.Events;

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

public class UserNameUpdatedEvent : DomainEvent
{
    public Guid UserId { get; }
    public string NewName { get; }

    public UserNameUpdatedEvent(Guid userId, string newName)
    {
        UserId = userId;
        NewName = newName;
    }
}
```

### 3. ایجاد Domain Event Handlers

```csharp
using Raziee.SharedKernel.Domain.Events;

public class UserCreatedEventHandler : IDomainEventHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger;

    public UserCreatedEventHandler(ILogger<UserCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(UserCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User {UserId} created with name {Name} and email {Email}", 
            domainEvent.UserId, domainEvent.Name, domainEvent.Email);
        
        // منطق کسب‌وکار خود را اینجا اضافه کنید
        // مثلاً ارسال ایمیل خوش‌آمدگویی، ایجاد audit log و غیره
        
        await Task.CompletedTask;
    }
}
```

## ایجاد Value Objects

```csharp
using Raziee.SharedKernel.Domain.ValueObjects;

public class Email : ValueObject
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("Email cannot be null or empty", nameof(value));
        
        if (!value.Contains("@"))
            throw new ArgumentException("Invalid email format", nameof(value));
        
        Value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(Email email) => email.Value;
    public static implicit operator Email(string value) => new Email(value);
}

public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string Country { get; }

    public Address(string street, string city, string country)
    {
        Street = street ?? throw new ArgumentNullException(nameof(street));
        City = city ?? throw new ArgumentNullException(nameof(city));
        Country = country ?? throw new ArgumentNullException(nameof(country));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return Country;
    }
}
```

## راه‌اندازی CQRS

### 1. ایجاد Commands

```csharp
using Raziee.SharedKernel.CQRS;
using FluentValidation;

public class CreateUserCommand : ICommand<CreateUserResult>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CreateUserResult
{
    public Guid UserId { get; set; }
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(100)
            .WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format");
    }
}
```

### 2. ایجاد Command Handlers

```csharp
using Raziee.SharedKernel.CQRS;
using Raziee.SharedKernel.Repositories;
using Raziee.SharedKernel.Data;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, CreateUserResult>
{
    private readonly IRepository<User, Guid> _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(
        IRepository<User, Guid> userRepository,
        IUnitOfWork unitOfWork)
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

### 3. ایجاد Queries

```csharp
using Raziee.SharedKernel.CQRS;

public class GetUserByIdQuery : IQuery<GetUserByIdResult>
{
    public Guid UserId { get; set; }
}

public class GetUserByIdResult
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, GetUserByIdResult>
{
    private readonly IReadRepository<User, Guid> _userRepository;

    public GetUserByIdQueryHandler(IReadRepository<User, Guid> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<GetUserByIdResult> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user == null)
            throw new EntityNotFoundException<User>(request.UserId);
        
        return new GetUserByIdResult
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }
}
```

## الگوی Repository

### 1. استفاده از Generic Repository

```csharp
public class UserService
{
    private readonly IRepository<User, Guid> _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IRepository<User, Guid> userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> CreateUserAsync(string name, string email)
    {
        var user = new User(Guid.NewGuid(), name, email);
        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();
        return user.Id;
    }

    public async Task<User?> GetUserAsync(Guid userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _userRepository.GetAllAsync();
    }
}
```

### 2. استفاده از Specifications

```csharp
using Raziee.SharedKernel.Specifications;

public class UserByEmailSpecification : BaseSpecification<User, Guid>
{
    public UserByEmailSpecification(string email)
    {
        AddCriteria(u => u.Email == email);
    }
}

public class UserByNameSpecification : BaseSpecification<User, Guid>
{
    public UserByNameSpecification(string name)
    {
        AddCriteria(u => u.Name.Contains(name));
        ApplyOrderBy(u => u.Name);
    }
}

// استفاده
public class UserService
{
    private readonly IRepository<User, Guid> _userRepository;

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        var specification = new UserByEmailSpecification(email);
        var users = await _userRepository.GetAsync(specification);
        return users.FirstOrDefault();
    }
}
```

## Domain Events

### 1. ثبت Event Handlers

```csharp
// در Program.cs یا Startup.cs
builder.Services.AddScoped<IDomainEventHandler<UserCreatedEvent>, UserCreatedEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<UserNameUpdatedEvent>, UserNameUpdatedEventHandler>();
```

### 2. مدیریت Events در Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<CreateUserResult>> CreateUser(CreateUserCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GetUserByIdResult>> GetUser(Guid id)
    {
        var query = new GetUserByIdQuery { UserId = id };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
```

## مراحل بعدی

حالا که اصول اولیه را راه‌اندازی کرده‌اید، می‌توانید موارد زیر را بررسی کنید:

1. **Multi-Tenancy**: یادگیری نحوه پیاده‌سازی برنامه‌های چند‌مستاجری
2. **Modular Monolith**: کشف نحوه ساخت modular monoliths با SharedKernel
3. **Microservices**: بررسی الگوها و انتزاعات میکروسرویس
4. **Advanced Patterns**: غوطه‌وری در الگوهای Saga، Circuit Breakers و بیشتر

### منابع اضافی

- [راهنمای معماری](architecture-fa.md)
- [مرجع API](api-reference.md)
- [نمونه‌ها](examples/)
- [مخزن GitHub](https://github.com/raziee/Raziee.SharedKernel)

### دریافت کمک

- 📖 بررسی [مستندات](docs/)
- 🐛 گزارش مشکلات در [GitHub](https://github.com/raziee/Raziee.SharedKernel/issues)
- 💬 شرکت در بحث‌ها در [GitHub Discussions](https://github.com/raziee/Raziee.SharedKernel/discussions)

برنامه‌نویسی موفق با Raziee.SharedKernel! 🚀
