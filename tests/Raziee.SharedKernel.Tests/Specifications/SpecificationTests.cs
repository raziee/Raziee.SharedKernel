using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Raziee.SharedKernel.Domain.Entities;
using Raziee.SharedKernel.Specifications;
using Xunit;

namespace Raziee.SharedKernel.Tests.Specifications;

public class SpecificationTests : IDisposable
{
    private readonly TestDbContext _context;

    public SpecificationTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
    }

    [Fact]
    public void Specification_WithCriteria_ShouldFilterEntities()
    {
        // Arrange
        var entities = new[]
        {
            new TestEntity(Guid.NewGuid(), "John", 25),
            new TestEntity(Guid.NewGuid(), "Jane", 30),
            new TestEntity(Guid.NewGuid(), "Bob", 35),
        };

        _context.TestEntities.AddRange(entities);
        _context.SaveChanges();

        var specification = new TestEntityByNameSpecification("John");

        // Act
        var query = _context.TestEntities.ApplySpecification(specification);
        var result = query.ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("John");
    }

    [Fact]
    public void Specification_WithMultipleCriteria_ShouldFilterEntities()
    {
        // Arrange
        var entities = new[]
        {
            new TestEntity(Guid.NewGuid(), "John", 25),
            new TestEntity(Guid.NewGuid(), "Jane", 30),
            new TestEntity(Guid.NewGuid(), "Bob", 35),
        };

        _context.TestEntities.AddRange(entities);
        _context.SaveChanges();

        var specification = new TestEntityByAgeRangeSpecification(25, 30);

        // Act
        var query = _context.TestEntities.ApplySpecification(specification);
        var result = query.ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(e => e.Name == "John");
        result.Should().Contain(e => e.Name == "Jane");
    }

    [Fact]
    public void Specification_WithOrderBy_ShouldOrderEntities()
    {
        // Arrange
        var entities = new[]
        {
            new TestEntity(Guid.NewGuid(), "Charlie", 30),
            new TestEntity(Guid.NewGuid(), "Alice", 25),
            new TestEntity(Guid.NewGuid(), "Bob", 35),
        };

        _context.TestEntities.AddRange(entities);
        _context.SaveChanges();

        var specification = new TestEntityOrderedByNameSpecification();

        // Act
        var query = _context.TestEntities.ApplySpecification(specification);
        var result = query.ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Alice");
        result[1].Name.Should().Be("Bob");
        result[2].Name.Should().Be("Charlie");
    }

    [Fact]
    public void Specification_WithPaging_ShouldReturnPagedResults()
    {
        // Arrange
        var entities = Enumerable
            .Range(1, 10)
            .Select(i => new TestEntity(Guid.NewGuid(), $"Name{i}", 20 + i))
            .ToArray();

        _context.TestEntities.AddRange(entities);
        _context.SaveChanges();

        var specification = new TestEntityPagedSpecification(2, 3);

        // Act
        var query = _context.TestEntities.ApplySpecification(specification);
        var result = query.ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Name4");
        result[1].Name.Should().Be("Name5");
        result[2].Name.Should().Be("Name6");
    }

    [Fact]
    public void Specification_WithNoTracking_ShouldDisableTracking()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Test", 25);
        _context.TestEntities.Add(entity);
        _context.SaveChanges();

        var specification = new TestEntityNoTrackingSpecification();

        // Act
        var query = _context.TestEntities.ApplySpecification(specification);
        var result = query.ToList();

        // Assert
        result.Should().HaveCount(1);
        _context.Entry(result.First()).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public void Specification_CombiningSpecifications_ShouldWork()
    {
        // Arrange
        var entities = new[]
        {
            new TestEntity(Guid.NewGuid(), "John", 25),
            new TestEntity(Guid.NewGuid(), "Jane", 30),
            new TestEntity(Guid.NewGuid(), "Bob", 35),
        };

        _context.TestEntities.AddRange(entities);
        _context.SaveChanges();

        var nameSpec = new TestEntityByNameSpecification("John");
        var ageSpec = new TestEntityByAgeRangeSpecification(20, 30);
        var combinedSpec = new TestEntityCombinedSpecification(nameSpec, ageSpec);

        // Act
        var query = _context.TestEntities.ApplySpecification(combinedSpec);
        var result = query.ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("John");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

// Test entity for specification tests
public class TestEntity : Entity<Guid>
{
    public string Name { get; private set; }
    public int Age { get; private set; }

    public TestEntity(Guid id, string name, int age)
        : base(id)
    {
        Name = name;
        Age = age;
    }
}

// Test specifications
public class TestEntityByNameSpecification : BaseSpecification<TestEntity, Guid>
{
    public TestEntityByNameSpecification(string name)
    {
        AddCriteria(e => e.Name == name);
    }
}

public class TestEntityByAgeRangeSpecification : BaseSpecification<TestEntity, Guid>
{
    public TestEntityByAgeRangeSpecification(int minAge, int maxAge)
    {
        AddCriteria(e => e.Age >= minAge && e.Age <= maxAge);
    }
}

public class TestEntityOrderedByNameSpecification : BaseSpecification<TestEntity, Guid>
{
    public TestEntityOrderedByNameSpecification()
    {
        ApplyOrderBy(e => e.Name);
    }
}

public class TestEntityPagedSpecification : BaseSpecification<TestEntity, Guid>
{
    public TestEntityPagedSpecification(int pageNumber, int pageSize)
    {
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }
}

public class TestEntityNoTrackingSpecification : BaseSpecification<TestEntity, Guid>
{
    public TestEntityNoTrackingSpecification()
    {
        ApplyTracking(false);
    }
}

public class TestEntityCombinedSpecification : BaseSpecification<TestEntity, Guid>
{
    public TestEntityCombinedSpecification(
        TestEntityByNameSpecification nameSpec,
        TestEntityByAgeRangeSpecification ageSpec
    )
    {
        AddCriteria(nameSpec.Criteria!);
        AddCriteria(ageSpec.Criteria!);
    }
}

// Test DbContext for specification tests
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options) { }

    public DbSet<TestEntity> TestEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Age).IsRequired();
        });
    }
}

// Extension method for applying specifications
public static class QueryableExtensions
{
    public static IQueryable<TEntity> ApplySpecification<TEntity, TId>(
        this IQueryable<TEntity> query,
        BaseSpecification<TEntity, TId> specification
    )
        where TEntity : Entity<TId>
        where TId : notnull
    {
        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        if (specification.ThenBy != null)
        {
            query = ((IOrderedQueryable<TEntity>)query).ThenBy(specification.ThenBy);
        }
        else if (specification.ThenByDescending != null)
        {
            query = ((IOrderedQueryable<TEntity>)query).ThenByDescending(
                specification.ThenByDescending
            );
        }

        if (specification.Skip > 0)
        {
            query = query.Skip(specification.Skip);
        }

        if (specification.Take > 0)
        {
            query = query.Take(specification.Take);
        }

        if (!specification.IsTrackingEnabled)
        {
            query = query.AsNoTracking();
        }

        return query;
    }
}
