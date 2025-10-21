# Getting Started with Raziee.SharedKernel

This guide will help you get started with Raziee.SharedKernel, a comprehensive DDD foundation library for .NET applications.

## Table of Contents

- [Installation](#installation)
- [Basic Setup](#basic-setup)
- [Creating Your First Entity](#creating-your-first-entity)
- [Creating Value Objects](#creating-value-objects)
- [Setting Up CQRS](#setting-up-cqrs)
- [Repository Pattern](#repository-pattern)
- [Domain Events](#domain-events)
- [Next Steps](#next-steps)

## Installation

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code
- Entity Framework Core (for data access)

### Install the Package

```bash
dotnet add package Raziee.SharedKernel
```

Or via Package Manager Console:

```powershell
Install-Package Raziee.SharedKernel
```

## Basic Setup

### 1. Configure Services

In your `Program.cs` or `Startup.cs`:

```csharp
using Raziee.SharedKernel.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add SharedKernel with default configuration
builder.Services.AddSharedKernel();

// Or configure specific features
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

### 2. Configure Entity Framework

```csharp
builder.Services.AddDbContext<YourDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure SharedKernel DbContext
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
        
        // Configure your entities
        modelBuilder.ConfigureAuditableEntities();
        modelBuilder.ConfigureSoftDelete();
        modelBuilder.ConfigureConcurrencyTokens();
    }
}
```

## Creating Your First Entity

### 1. Create a Domain Entity

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
        
        // Raise domain event
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

### 2. Create Domain Events

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

### 3. Create Domain Event Handlers

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
        
        // Add your business logic here
        // e.g., send welcome email, create audit log, etc.
        
        await Task.CompletedTask;
    }
}
```

## Creating Value Objects

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

## Setting Up CQRS

### 1. Create Commands

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

### 2. Create Command Handlers

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

### 3. Create Queries

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

## Repository Pattern

### 1. Using Generic Repository

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

### 2. Using Specifications

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

// Usage
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

### 1. Register Event Handlers

```csharp
// In Program.cs or Startup.cs
builder.Services.AddScoped<IDomainEventHandler<UserCreatedEvent>, UserCreatedEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<UserNameUpdatedEvent>, UserNameUpdatedEventHandler>();
```

### 2. Handle Events in Controllers

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

## Next Steps

Now that you have the basics set up, you can explore:

1. **Multi-Tenancy**: Learn how to implement multi-tenant applications
2. **Modular Monolith**: Discover how to build modular monoliths with SharedKernel
3. **Microservices**: Explore microservices patterns and abstractions
4. **Advanced Patterns**: Dive into Saga patterns, Circuit Breakers, and more

### Additional Resources

- [Architecture Guide](architecture.md)
- [API Reference](api-reference.md)
- [Examples](examples/)
- [GitHub Repository](https://github.com/raziee/Raziee.SharedKernel)

### Getting Help

- 📖 Check the [documentation](docs/)
- 🐛 Report issues on [GitHub](https://github.com/raziee/Raziee.SharedKernel/issues)
- 💬 Join discussions in [GitHub Discussions](https://github.com/raziee/Raziee.SharedKernel/discussions)

Happy coding with Raziee.SharedKernel! 🚀
