using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Raziee.SharedKernel.CQRS;
using Xunit;

namespace Raziee.SharedKernel.Tests.CQRS;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task ValidationBehavior_WithValidRequest_ShouldPass()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<
            IPipelineBehavior<TestRequest, TestResponse>,
            ValidationBehavior<TestRequest, TestResponse>
        >();
        services.AddScoped<IValidator<TestRequest>, TestValidator>();

        var serviceProvider = services.BuildServiceProvider();
        var behavior = serviceProvider.GetRequiredService<
            IPipelineBehavior<TestRequest, TestResponse>
        >();

        var request = new TestRequest { Name = "Test" };
        var next = new Mock<RequestHandlerDelegate<TestResponse>>();
        next.Setup(x => x()).ReturnsAsync(new TestResponse { Result = "Success" });

        // Act
        var result = await behavior.Handle(request, next.Object, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().Be("Success");
        next.Verify(x => x(), Times.Once);
    }

    [Fact]
    public async Task ValidationBehavior_WithInvalidRequest_ShouldThrowValidationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<
            IPipelineBehavior<TestRequest, TestResponse>,
            ValidationBehavior<TestRequest, TestResponse>
        >();
        services.AddScoped<IValidator<TestRequest>, TestValidator>();

        var serviceProvider = services.BuildServiceProvider();
        var behavior = serviceProvider.GetRequiredService<
            IPipelineBehavior<TestRequest, TestResponse>
        >();

        var request = new TestRequest { Name = "" }; // Invalid
        var next = new Mock<RequestHandlerDelegate<TestResponse>>();

        // Act & Assert
        var action = async () =>
            await behavior.Handle(request, next.Object, CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
        next.Verify(x => x(), Times.Never);
    }

    [Fact]
    public async Task ValidationBehavior_WithNoValidators_ShouldPass()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<
            IPipelineBehavior<TestRequest, TestResponse>,
            ValidationBehavior<TestRequest, TestResponse>
        >();

        var serviceProvider = services.BuildServiceProvider();
        var behavior = serviceProvider.GetRequiredService<
            IPipelineBehavior<TestRequest, TestResponse>
        >();

        var request = new TestRequest { Name = "Test" };
        var next = new Mock<RequestHandlerDelegate<TestResponse>>();
        next.Setup(x => x()).ReturnsAsync(new TestResponse { Result = "Success" });

        // Act
        var result = await behavior.Handle(request, next.Object, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().Be("Success");
        next.Verify(x => x(), Times.Once);
    }

    public class TestRequest : MediatR.IRequest<TestResponse>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }

    public class TestValidator : AbstractValidator<TestRequest>
    {
        public TestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        }
    }
}
