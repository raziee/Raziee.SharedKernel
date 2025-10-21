using FluentAssertions;
using Raziee.SharedKernel.Domain.ValueObjects;
using Xunit;

namespace Raziee.SharedKernel.Tests.Domain.ValueObjects;

public class ValueObjectTests
{
    [Fact]
    public void ValueObject_WithSameComponents_ShouldBeEqual()
    {
        // Arrange
        var valueObject1 = new TestValueObject("test", 123);
        var valueObject2 = new TestValueObject("test", 123);

        // Act & Assert
        valueObject1.Should().Be(valueObject2);
        valueObject1.GetHashCode().Should().Be(valueObject2.GetHashCode());
    }

    [Fact]
    public void ValueObject_WithDifferentComponents_ShouldNotBeEqual()
    {
        // Arrange
        var valueObject1 = new TestValueObject("test", 123);
        var valueObject2 = new TestValueObject("test", 456);

        // Act & Assert
        valueObject1.Should().NotBe(valueObject2);
    }

    [Fact]
    public void ValueObject_ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var valueObject = new TestValueObject("test", 123);

        // Act
        var result = valueObject.ToString();

        // Assert
        result.Should().Be("TestValueObject[test, 123]");
    }

    private class TestValueObject : ValueObject
    {
        public string Name { get; }
        public int Value { get; }

        public TestValueObject(string name, int value)
        {
            Name = name;
            Value = value;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
            yield return Value;
        }
    }
}
