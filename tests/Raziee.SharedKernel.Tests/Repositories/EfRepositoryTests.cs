using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Raziee.SharedKernel.Domain.Entities;
using Raziee.SharedKernel.Repositories;
using Xunit;

namespace Raziee.SharedKernel.Tests.Repositories;

public class EfRepositoryTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly EfRepository<TestEntity, Guid> _repository;
    private readonly Mock<ILogger<EfRepository<TestEntity, Guid>>> _loggerMock;

    public EfRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _loggerMock = new Mock<ILogger<EfRepository<TestEntity, Guid>>>();
        _repository = new EfRepository<TestEntity, Guid>(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnEntity()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Test Name");
        await _context.TestEntities.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Name.Should().Be(entity.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(invalidId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdsAsync_WithValidIds_ShouldReturnEntities()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid(), "Test 1");
        var entity2 = new TestEntity(Guid.NewGuid(), "Test 2");
        var entity3 = new TestEntity(Guid.NewGuid(), "Test 3");
        
        await _context.TestEntities.AddRangeAsync(entity1, entity2, entity3);
        await _context.SaveChangesAsync();

        var ids = new[] { entity1.Id, entity2.Id };

        // Act
        var result = await _repository.GetByIdsAsync(ids);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(e => e.Id == entity1.Id);
        result.Should().Contain(e => e.Id == entity2.Id);
    }

    [Fact]
    public async Task GetAllAsync_WithEntities_ShouldReturnAllEntities()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid(), "Test 1");
        var entity2 = new TestEntity(Guid.NewGuid(), "Test 2");
        
        await _context.TestEntities.AddRangeAsync(entity1, entity2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(e => e.Id == entity1.Id);
        result.Should().Contain(e => e.Id == entity2.Id);
    }

    [Fact]
    public async Task GetPagedAsync_WithValidParameters_ShouldReturnPagedResult()
    {
        // Arrange
        var entities = Enumerable.Range(1, 10)
            .Select(i => new TestEntity(Guid.NewGuid(), $"Test {i}"))
            .ToList();
        
        await _context.TestEntities.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPagedAsync(2, 3);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(3);
        result.TotalCount.Should().Be(10);
        result.TotalPages.Should().Be(4);
    }

    [Fact]
    public async Task CountAsync_WithEntities_ShouldReturnCorrectCount()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid(), "Test 1");
        var entity2 = new TestEntity(Guid.NewGuid(), "Test 2");
        
        await _context.TestEntities.AddRangeAsync(entity1, entity2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task ExistsAsync_WithExistingId_ShouldReturnTrue()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Test Name");
        await _context.TestEntities.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(entity.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.ExistsAsync(nonExistingId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AnyAsync_WithEntities_ShouldReturnTrue()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Test Name");
        await _context.TestEntities.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.AnyAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AnyAsync_WithoutEntities_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.AnyAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_WithValidEntity_ShouldAddEntity()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Test Name");

        // Act
        await _repository.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.TestEntities.FindAsync(entity.Id);
        result.Should().NotBeNull();
        result!.Name.Should().Be(entity.Name);
    }

    [Fact]
    public async Task AddRangeAsync_WithValidEntities_ShouldAddEntities()
    {
        // Arrange
        var entities = new[]
        {
            new TestEntity(Guid.NewGuid(), "Test 1"),
            new TestEntity(Guid.NewGuid(), "Test 2")
        };

        // Act
        await _repository.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.TestEntities.CountAsync();
        result.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_WithValidEntity_ShouldUpdateEntity()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Original Name");
        await _context.TestEntities.AddAsync(entity);
        await _context.SaveChangesAsync();

        entity.ChangeName("Updated Name");

        // Act
        await _repository.UpdateAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.TestEntities.FindAsync(entity.Id);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task DeleteAsync_WithValidEntity_ShouldDeleteEntity()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Test Name");
        await _context.TestEntities.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.TestEntities.FindAsync(entity.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteByIdAsync_WithValidId_ShouldDeleteEntity()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Test Name");
        await _context.TestEntities.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteByIdAsync(entity.Id);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.TestEntities.FindAsync(entity.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteByIdAsync_WithInvalidId_ShouldNotThrow()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act & Assert
        var action = async () => await _repository.DeleteByIdAsync(invalidId);
        await action.Should().NotThrowAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

// Test entity for repository tests
public class TestEntity : Entity<Guid>
{
    public string Name { get; private set; }

    public TestEntity(Guid id, string name) : base(id)
    {
        Name = name;
    }

    public void ChangeName(string newName)
    {
        Name = newName;
    }
}

// Test DbContext for repository tests
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<TestEntity> TestEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });
    }
}
