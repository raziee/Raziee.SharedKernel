using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Raziee.SharedKernel.CQRS;
using Raziee.SharedKernel.Data;
using Xunit;

namespace Raziee.SharedKernel.Tests.CQRS;

public class TransactionBehaviorTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<TransactionBehavior<TestRequest, TestResponse>>> _loggerMock;
    private readonly TransactionBehavior<TestRequest, TestResponse> _behavior;

    public TransactionBehaviorTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<TransactionBehavior<TestRequest, TestResponse>>>();
        _behavior = new TransactionBehavior<TestRequest, TestResponse>(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithoutActiveTransaction_ShouldBeginAndCommitTransaction()
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.HasActiveTransaction).Returns(false);
        var request = new TestRequest();
        var response = new TestResponse { Value = "Test" };
        var next = new Mock<Raziee.SharedKernel.CQRS.RequestHandlerDelegate<TestResponse>>();
        next.Setup(x => x()).ReturnsAsync(response);

        // Act
        var result = await _behavior.Handle(request, next.Object, CancellationToken.None);

        // Assert
        result.Should().Be(response);
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task Handle_WithActiveTransaction_ShouldNotBeginNewTransaction()
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.HasActiveTransaction).Returns(true);
        var request = new TestRequest();
        var response = new TestResponse { Value = "Test" };
        var next = new Mock<Raziee.SharedKernel.CQRS.RequestHandlerDelegate<TestResponse>>();
        next.Setup(x => x()).ReturnsAsync(response);

        // Act
        var result = await _behavior.Handle(request, next.Object, CancellationToken.None);

        // Assert
        result.Should().Be(response);
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(CancellationToken.None), Times.Never);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(CancellationToken.None), Times.Never);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNextThrowsException_ShouldRollbackTransaction()
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.HasActiveTransaction).Returns(false);
        var request = new TestRequest();
        var exception = new InvalidOperationException("Test exception");
        var next = new Mock<Raziee.SharedKernel.CQRS.RequestHandlerDelegate<TestResponse>>();
        next.Setup(x => x()).ThrowsAsync(exception);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _behavior.Handle(request, next.Object, CancellationToken.None));

        thrownException.Should().Be(exception);
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToUnitOfWork()
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.HasActiveTransaction).Returns(false);
        var request = new TestRequest();
        var response = new TestResponse { Value = "Test" };
        var next = new Mock<Raziee.SharedKernel.CQRS.RequestHandlerDelegate<TestResponse>>();
        next.Setup(x => x()).ReturnsAsync(response);
        var cancellationToken = new CancellationToken();

        // Act
        var result = await _behavior.Handle(request, next.Object, cancellationToken);

        // Assert
        result.Should().Be(response);
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(cancellationToken), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(cancellationToken), Times.Once);
    }
}
