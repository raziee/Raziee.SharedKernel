using System.Linq.Expressions;
using FluentAssertions;
using Raziee.SharedKernel.Domain.Entities;
using Raziee.SharedKernel.Specifications;
using Xunit;

namespace Raziee.SharedKernel.Tests.Specifications;

public class BaseSpecificationTests
{
    [Fact]
    public void BaseSpecification_ShouldStartWithEmptyCriteria()
    {
        // Arrange & Act
        var specification = new TestSpecification();

        // Assert
        specification.Criteria.Should().BeNull();
        specification.Includes.Should().BeEmpty();
        specification.IncludeStrings.Should().BeEmpty();
        specification.OrderBy.Should().BeNull();
        specification.OrderByDescending.Should().BeNull();
        specification.Take.Should().Be(0);
        specification.Skip.Should().Be(0);
        specification.IsTrackingEnabled.Should().BeTrue();
        specification.IsIgnoreQueryFilters.Should().BeFalse();
    }

    [Fact]
    public void BaseSpecification_AddCriteria_ShouldSetCriteria()
    {
        // Arrange
        var specification = new TestSpecification();

        // Act
        specification.AddCriteria(x => x.Id != Guid.Empty);

        // Assert
        specification.Criteria.Should().NotBeNull();
    }

    [Fact]
    public void BaseSpecification_AddInclude_ShouldAddInclude()
    {
        // Arrange
        var specification = new TestSpecification();

        // Act
        specification.AddInclude(x => x.Id);

        // Assert
        specification.Includes.Should().HaveCount(1);
    }

    [Fact]
    public void BaseSpecification_AddIncludeString_ShouldAddIncludeString()
    {
        // Arrange
        var specification = new TestSpecification();

        // Act
        specification.AddInclude("RelatedEntity");

        // Assert
        specification.IncludeStrings.Should().Contain("RelatedEntity");
    }

    [Fact]
    public void BaseSpecification_ApplyOrderBy_ShouldSetOrderBy()
    {
        // Arrange
        var specification = new TestSpecification();

        // Act
        specification.ApplyOrderBy(x => x.Id);

        // Assert
        specification.OrderBy.Should().NotBeNull();
    }

    [Fact]
    public void BaseSpecification_ApplyPaging_ShouldSetPaging()
    {
        // Arrange
        var specification = new TestSpecification();

        // Act
        specification.ApplyPaging(10, 5);

        // Assert
        specification.Skip.Should().Be(10);
        specification.Take.Should().Be(5);
    }

    [Fact]
    public void BaseSpecification_ApplyTracking_ShouldSetTracking()
    {
        // Arrange
        var specification = new TestSpecification();

        // Act
        specification.ApplyTracking(false);

        // Assert
        specification.IsTrackingEnabled.Should().BeFalse();
    }

    [Fact]
    public void BaseSpecification_ApplyIgnoreQueryFilters_ShouldSetIgnoreQueryFilters()
    {
        // Arrange
        var specification = new TestSpecification();

        // Act
        specification.ApplyIgnoreQueryFilters(true);

        // Assert
        specification.IsIgnoreQueryFilters.Should().BeTrue();
    }

    private class TestSpecification : BaseSpecification<TestEntity, Guid>
    {
        public new void AddCriteria(Expression<Func<TestEntity, bool>> criteria)
        {
            base.AddCriteria(criteria);
        }

        public new void AddInclude(Expression<Func<TestEntity, object>> includeExpression)
        {
            base.AddInclude(includeExpression);
        }

        public new void AddInclude(string includeString)
        {
            base.AddInclude(includeString);
        }

        public new void ApplyOrderBy(Expression<Func<TestEntity, object>> orderByExpression)
        {
            base.ApplyOrderBy(orderByExpression);
        }

        public new void ApplyPaging(int skip, int take)
        {
            base.ApplyPaging(skip, take);
        }

        public new void ApplyTracking(bool isTrackingEnabled)
        {
            base.ApplyTracking(isTrackingEnabled);
        }

        public new void ApplyIgnoreQueryFilters(bool isIgnoreQueryFilters)
        {
            base.ApplyIgnoreQueryFilters(isIgnoreQueryFilters);
        }
    }

    private class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) : base(id)
        {
        }
    }
}
