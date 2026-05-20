using Shared.Application.Cqrs.Core.Behaviours;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Testing.Doubles.Logging;

namespace Shared.Application.Cqrs.Core.Tests.Behaviours;

/// <summary>
/// Тесты <see cref="LoggingPipelineBehaviour{TRequest,TResponse}"/>.
/// Проверяют вызов делегата <c>Next</c>, возврат его результата,
/// однократность вызова и проброс <see cref="CancellationToken"/>.
/// </summary>
public sealed class LoggingPipelineBehaviourTests
{
    /// <summary>
    /// Создаёт экземпляр pipeline behaviour с опциональным логгером.
    /// </summary>
    private static LoggingPipelineBehaviour<TestRequest, TestResponse> CreateSut(
        FakeLogger<LoggingPipelineBehaviour<TestRequest, TestResponse>>? logger = null)
    {
        return new LoggingPipelineBehaviour<TestRequest, TestResponse>(
            logger ?? new FakeLogger<LoggingPipelineBehaviour<TestRequest, TestResponse>>(new FakeLogger()));
    }

    #region Next Delegate Tests

    /// <summary>
    /// Behavior вызывает делегат <c>Next</c> ровно один раз.
    /// </summary>
    [Fact]
    public async Task Handle_CallsNextDelegate()
    {
        // Arrange
        var sut = CreateSut();
        var request = new TestRequest();
        var response = new TestResponse();
        var called = false;

        Task<TestResponse> Next()
        {
            called = true;
            return Task.FromResult(response);
        }

        // Act
        var result = await sut.Handle(request, Next, TestContext.Current.CancellationToken);

        // Assert
        called.Should().BeTrue();
        result.Should().Be(response);
    }

    /// <summary>
    /// Behavior возвращает тот же ответ, что и делегат <c>Next</c>.
    /// </summary>
    [Fact]
    public async Task Handle_ReturnsResponseFromNext()
    {
        // Arrange
        var sut = CreateSut();
        var request = new TestRequest();
        var expected = new TestResponse();

        Task<TestResponse> Next() => Task.FromResult(expected);

        // Act
        var result = await sut.Handle(request, Next, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Делегат <c>Next</c> вызывается ровно один раз
    /// (счётчик инкрементируется единожды).
    /// </summary>
    [Fact]
    public async Task Handle_NextDelegateIsInvokedOnce()
    {
        // Arrange
        var sut = CreateSut();
        var request = new TestRequest();
        var response = new TestResponse();
        var counter = 0;

        Task<TestResponse> Next()
        {
            counter++;
            return Task.FromResult(response);
        }

        // Act
        await sut.Handle(request, Next, TestContext.Current.CancellationToken);

        // Assert
        counter.Should().Be(1);
    }

    #endregion

    #region CancellationToken Tests

    /// <summary>
    /// При отменённом <see cref="CancellationToken"/>
    /// behavior пробрасывает <see cref="OperationCanceledException"/>.
    /// </summary>
    [Fact]
    public async Task Handle_ForwardsCancellationTokenToNext()
    {
        // Arrange
        var sut = CreateSut();
        var request = new TestRequest();
        var response = new TestResponse();

        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        await cts.CancelAsync();

        // Act
        var action = () => sut.Handle(request, Next, token);

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
        return;

        Task<TestResponse> Next() => Task.FromResult(response);
    }

    #endregion
}
