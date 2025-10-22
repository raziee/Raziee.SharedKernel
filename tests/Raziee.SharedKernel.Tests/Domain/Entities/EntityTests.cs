using FluentAssertions;
using Raziee.SharedKernel.Domain.Entities;
using Xunit;

namespace Raziee.SharedKernel.Tests.Domain.Entities;

public class EntityTests
{
    [Fact]
    public void Entity_WithSameId_ShouldBeEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act & Assert
        entity1.Should().Be(entity2);
        entity1.GetHashCode().Should().Be(entity2.GetHashCode());
    }

    [Fact]
    public void Entity_WithDifferentId_ShouldNotBeEqual()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid());
        var entity2 = new TestEntity(Guid.NewGuid());

        // Act & Assert
        entity1.Should().NotBe(entity2);
    }

    [Fact]
    public void Entity_WithValidId_ShouldNotThrowException()
    {
        // Act & Assert
        var action = () => new TestEntity(Guid.NewGuid());
        action.Should().NotThrow();
    }

    [Fact]
    public void Entity_ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new TestEntity(id);

        // Act
        var result = entity.ToString();

        // Assert
        result.Should().Be($"TestEntity[Id={id}]");
    }

    private class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id)
            : base(id) { }
    }
}
