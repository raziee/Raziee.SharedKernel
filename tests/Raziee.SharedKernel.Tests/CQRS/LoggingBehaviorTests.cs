using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Raziee.SharedKernel.CQRS;
using Xunit;

namespace Raziee.SharedKernel.Tests.CQRS;

public class LoggingBehaviorTests
{
    private readonly Mock<ILogger<LoggingBehavior<TestRequest, TestResponse>>> _loggerMock;
    private readonly LoggingBehavior<TestRequest, TestResponse> _behavior;

    public LoggingBehaviorTests()
    {
        _loggerMock = new Mock<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
        _behavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldLogRequestAndResponse()
    {
        // Arrange
        var request = new TestRequest { Value = "Test Request" };
        var response = new TestResponse { Value = "Test Response" };
        var next = new Mock<SharedKernel.CQRS.RequestHandlerDelegate<TestResponse>>();
        next.Setup(x => x()).ReturnsAsync(response);

        // Act
        var result = await _behavior.Handle(request, next.Object, CancellationToken.None);

        // Assert
        result.Should().Be(response);

        // Verify that logging was called
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );

        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Response")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WhenNextThrowsException_ShouldLogException()
    {
        // Arrange
        var request = new TestRequest { Value = "Test Request" };
        var exception = new InvalidOperationException("Test exception");
        var next = new Mock<SharedKernel.CQRS.RequestHandlerDelegate<TestResponse>>();
        next.Setup(x => x()).ThrowsAsync(exception);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _behavior.Handle(request, next.Object, CancellationToken.None)
        );

        thrownException.Should().Be(exception);

        // Verify that error logging was called
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToNext()
    {
        // Arrange
        var request = new TestRequest { Value = "Test Request" };
        var response = new TestResponse { Value = "Test Response" };
        var next = new Mock<SharedKernel.CQRS.RequestHandlerDelegate<TestResponse>>();
        next.Setup(x => x()).ReturnsAsync(response);
        var cancellationToken = new CancellationToken();

        // Act
        var result = await _behavior.Handle(request, next.Object, cancellationToken);

        // Assert
        result.Should().Be(response);
        next.Verify(x => x(), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullRequest_ShouldNotThrow()
    {
        // Arrange
        TestRequest? request = null;
        var response = new TestResponse { Value = "Test Response" };
        var next = new Mock<SharedKernel.CQRS.RequestHandlerDelegate<TestResponse>>();
        next.Setup(x => x()).ReturnsAsync(response);

        // Act & Assert
        var action = async () =>
            await _behavior.Handle(request!, next.Object, CancellationToken.None);
        await action.Should().NotThrowAsync();
    }
}

public class TestRequest : IRequest<TestResponse>
{
    public string Value { get; set; } = string.Empty;
}

public class TestResponse
{
    public string Value { get; set; } = string.Empty;
}
