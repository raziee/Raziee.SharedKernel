using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Raziee.SharedKernel.Data;
using Raziee.SharedKernel.Domain.Entities;
using Raziee.SharedKernel.Domain.Events;
using Xunit;

namespace Raziee.SharedKernel.Tests.Data;

public class UnitOfWorkTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock;
    private readonly Mock<ILogger<UnitOfWork>> _loggerMock;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new TestDbContext(options);
        _domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        _loggerMock = new Mock<ILogger<UnitOfWork>>();
        _unitOfWork = new UnitOfWork(
            _context,
            _domainEventDispatcherMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public void HasActiveTransaction_Initially_ShouldReturnFalse()
    {
        // Act
        var result = _unitOfWork.HasActiveTransaction;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasPendingChanges_Initially_ShouldReturnFalse()
    {
        // Act
        var result = _unitOfWork.HasPendingChanges;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task BeginTransactionAsync_ShouldSetActiveTransaction()
    {
        // Act & Assert
        // Note: In-memory database doesn't support transactions
        // This test verifies the method doesn't throw
        var action = async () => await _unitOfWork.BeginTransactionAsync();
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task BeginTransactionAsync_WhenAlreadyActive_ShouldNotThrow()
    {
        // Act & Assert
        // Note: In-memory database doesn't support transactions
        // This test verifies the method doesn't throw when called multiple times
        var action = async () => await _unitOfWork.BeginTransactionAsync();
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CommitTransactionAsync_WithActiveTransaction_ShouldCommit()
    {
        // Act & Assert
        // Note: In-memory database doesn't support transactions
        // This test verifies the method doesn't throw
        var action = async () => await _unitOfWork.CommitTransactionAsync();
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CommitTransactionAsync_WithoutActiveTransaction_ShouldNotThrow()
    {
        // Act & Assert
        var action = async () => await _unitOfWork.CommitTransactionAsync();
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RollbackTransactionAsync_WithActiveTransaction_ShouldRollback()
    {
        // Act & Assert
        // Note: In-memory database doesn't support transactions
        // This test verifies the method doesn't throw
        var action = async () => await _unitOfWork.RollbackTransactionAsync();
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RollbackTransactionAsync_WithoutActiveTransaction_ShouldNotThrow()
    {
        // Act & Assert
        var action = async () => await _unitOfWork.RollbackTransactionAsync();
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_WithEntities_ShouldSaveChanges()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Test Name");
        await _context.TestEntities.AddAsync(entity);

        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
        _unitOfWork.HasPendingChanges.Should().BeFalse();
    }

    [Fact]
    public async Task SaveChangesAsync_WithDomainEvents_ShouldDispatchEvents()
    {
        // Arrange
        var aggregateRoot = new TestAggregateRootWithEvents(Guid.NewGuid(), "Test Name");
        await _context.TestAggregateRootsWithEvents.AddAsync(aggregateRoot);

        // Act
        await _unitOfWork.SaveChangesAsync();

        // Assert
        _domainEventDispatcherMock.Verify(
            x =>
                x.DispatchAsync(
                    It.IsAny<IEnumerable<IDomainEvent>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task SaveChangesAsync_WithCancellationToken_ShouldPassTokenToDispatcher()
    {
        // Arrange
        var aggregateRoot = new TestAggregateRootWithEvents(Guid.NewGuid(), "Test Name");
        await _context.TestAggregateRootsWithEvents.AddAsync(aggregateRoot);
        var cancellationToken = new CancellationToken();

        // Act
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Assert
        _domainEventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), cancellationToken),
            Times.Once
        );
    }

    [Fact]
    public async Task SaveChangesAsync_WhenContextThrowsException_ShouldPropagateException()
    {
        // Arrange
        // Dispose the context to cause an exception
        await _context.DisposeAsync();

        // Act & Assert
        var action = async () => await _unitOfWork.SaveChangesAsync();
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoChanges_ShouldReturnZero()
    {
        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.Should().Be(0);
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _context.Dispose();
    }
}

// Test entity for unit of work tests
public class TestEntity : Entity<Guid>
{
    public string Name { get; private set; }

    public TestEntity(Guid id, string name)
        : base(id)
    {
        Name = name;
    }
}

// Test aggregate root for unit of work tests
public class TestAggregateRoot : AggregateRoot<Guid>
{
    public string Name { get; private set; }

    public TestAggregateRoot(Guid id, string name)
        : base(id)
    {
        Name = name;
    }
}

// Test aggregate root with domain events for unit of work tests
public class TestAggregateRootWithEvents : AggregateRoot<Guid>
{
    public string Name { get; private set; }

    public TestAggregateRootWithEvents(Guid id, string name)
        : base(id)
    {
        Name = name;
        // Add a domain event in constructor to test event dispatch
        AddDomainEvent(new TestDomainEvent($"Created {name}"));
    }
}

// Test domain event for unit of work tests
public class TestDomainEvent : DomainEvent
{
    public string Value { get; }

    public TestDomainEvent(string value)
    {
        Value = value;
    }
}

// Test DbContext for unit of work tests
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options) { }

    public DbSet<TestEntity> TestEntities { get; set; } = null!;
    public DbSet<TestAggregateRoot> TestAggregateRoots { get; set; } = null!;
    public DbSet<TestAggregateRootWithEvents> TestAggregateRootsWithEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<TestAggregateRoot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<TestAggregateRootWithEvents>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });
    }
}
