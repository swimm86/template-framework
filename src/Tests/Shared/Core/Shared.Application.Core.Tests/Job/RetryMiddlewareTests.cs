// ----------------------------------------------------------------------------------------------
// <copyright file="RetryMiddlewareTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Job.Pipeline;
using Shared.Application.Core.Job.Pipeline.Middlewares;
using Shared.Application.Core.Tests.Support;
using Shared.Testing.Doubles.Logging;

namespace Shared.Application.Core.Tests.Job;

/// <summary>
/// Тесты <see cref="RetryMiddleware"/>: успех с первой попытки, retry до <c>MaxAttempts</c>,
/// rethrow после исчерпания попыток, чтение <see cref="RetryOptions"/> из
/// <see cref="ScheduledJobContext"/> (а не из <c>IOptions</c>) и поведение
/// при <c>RetryOptions = null</c>.
/// </summary>
public sealed class RetryMiddlewareTests
{
    /// <summary>
    /// При успехе с первой попытки retry не выполняется.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_SuccessOnFirstAttempt_DoesNotRetry()
    {
        // Arrange
        var logger = new FakeLogger();
        var middleware = new RetryMiddleware(new FakeLogger<RetryMiddleware>(logger));
        var sp = new ServiceCollection().BuildServiceProvider();
        var ctx = new ScheduledJobContext("k", sp, TestContext.Current.CancellationToken)
        {
            RetryOptions = RetryTestSupport.DefaultOptions(),
        };

        var attempts = 0;

        // Act
        await middleware.InvokeAsync(ctx, Next);

        // Assert
        attempts.Should().Be(1);
        return;

        Task Next(ScheduledJobContext _)
        {
            attempts++;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// При неуспехе на каждой попытке retry повторяет до <c>MaxAttempts</c>.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_AlwaysFails_RetriesUpToMaxAttempts()
    {
        // Arrange
        var logger = new FakeLogger();
        var middleware = new RetryMiddleware(new FakeLogger<RetryMiddleware>(logger));
        var sp = new ServiceCollection().BuildServiceProvider();
        var ctx = new ScheduledJobContext("k", sp, TestContext.Current.CancellationToken)
        {
            RetryOptions = RetryTestSupport.DefaultOptions(),
        };

        var attempts = 0;
        ScheduledJobDelegate next = _ =>
        {
            attempts++;
            throw new InvalidOperationException("boom");
        };

        // Act
        var act = () => middleware.InvokeAsync(ctx, next);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        attempts.Should().Be(3);
    }

    /// <summary>
    /// Успех со второй попытки — middleware не пробрасывает исключение.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_SuccessOnSecondAttempt_StopsRetrying()
    {
        // Arrange
        var logger = new FakeLogger();
        var middleware = new RetryMiddleware(new FakeLogger<RetryMiddleware>(logger));
        var sp = new ServiceCollection().BuildServiceProvider();
        var ctx = new ScheduledJobContext("k", sp, TestContext.Current.CancellationToken)
        {
            RetryOptions = RetryTestSupport.DefaultOptions(),
        };

        var attempts = 0;

        // Act
        await middleware.InvokeAsync(ctx, Next);

        // Assert
        attempts.Should().Be(2);
        return;

        Task Next(ScheduledJobContext _)
        {
            attempts++;
            return attempts < 2 ? throw new InvalidOperationException("boom") : Task.CompletedTask;
        }
    }

    /// <summary>
    /// При <c>MaxAttempts = 1</c> исключение пробрасывается с первой же попытки,
    /// никакого retry не происходит.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_MaxAttemptsOne_ThrowsImmediately()
    {
        // Arrange
        var logger = new FakeLogger();
        var middleware = new RetryMiddleware(new FakeLogger<RetryMiddleware>(logger));
        var sp = new ServiceCollection().BuildServiceProvider();
        var ctx = new ScheduledJobContext("k", sp, TestContext.Current.CancellationToken)
        {
            RetryOptions = RetryTestSupport.WithMaxAttempts(1),
        };

        var attempts = 0;
        ScheduledJobDelegate next = _ =>
        {
            attempts++;
            throw new InvalidOperationException("boom");
        };

        // Act
        var act = () => middleware.InvokeAsync(ctx, next);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        attempts.Should().Be(1);
    }

    /// <summary>
    /// На каждый неуспешный attempt (кроме последнего) middleware логирует
    /// <c>Warning</c> с номером попытки и общим <c>MaxAttempts</c>.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_OnFailure_LogsWarningWithAttemptNumber()
    {
        // Arrange
        var logger = new FakeLogger();
        var middleware = new RetryMiddleware(new FakeLogger<RetryMiddleware>(logger));
        var sp = new ServiceCollection().BuildServiceProvider();
        var ctx = new ScheduledJobContext("billing-job", sp, TestContext.Current.CancellationToken)
        {
            RetryOptions = RetryTestSupport.DefaultOptions(),
        };

        ScheduledJobDelegate next = _ => throw new InvalidOperationException("boom");

        // Act
        var act = () => middleware.InvokeAsync(ctx, next);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        // Должно быть ровно MaxAttempts - 1 = 2 Warning-записи (после 1-й и 2-й попыток).
        var warnings = logger.Entries.Where(e => e.Level == LogLevel.Warning).ToList();
        warnings.Should().HaveCount(2);
        warnings[0].Message.Should().Contain("billing-job");
        warnings[0].Message.Should().Contain("1/3");
        warnings[1].Message.Should().Contain("2/3");
    }

    /// <summary>
    /// Middleware выдерживает <see cref="RetryOptions.Delay"/> между попытками.
    /// При <c>Delay = 200ms</c> и двух попытках общее время должно быть не менее ~200ms.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_RespectsConfiguredDelayBetweenAttempts()
    {
        // Arrange
        var logger = new FakeLogger();
        var delay = TimeSpan.FromMilliseconds(200);
        var middleware = new RetryMiddleware(new FakeLogger<RetryMiddleware>(logger));
        var sp = new ServiceCollection().BuildServiceProvider();
        var ctx = new ScheduledJobContext("slow", sp, TestContext.Current.CancellationToken)
        {
            RetryOptions = new RetryOptions
            {
                MaxAttempts = 2,
                Delay = delay,
            },
        };

        var attempts = 0;
        ScheduledJobDelegate next = _ =>
        {
            attempts++;
            throw new InvalidOperationException("boom");
        };

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var act = () => middleware.InvokeAsync(ctx, next);
        await act.Should().ThrowAsync<InvalidOperationException>();
        sw.Stop();

        // Assert
        attempts.Should().Be(2);
        sw.Elapsed.Should().BeGreaterThanOrEqualTo(delay,
            "middleware должен выждать Delay между попытками");
    }

    /// <summary>
    /// Если <see cref="CancellationToken"/> отменяется во время <c>Task.Delay</c>
    /// между попытками, middleware пробрасывает <see cref="OperationCanceledException"/>
    /// и не делает следующую попытку.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_CancellationDuringDelay_ThrowsOperationCanceled()
    {
        // Arrange
        var logger = new FakeLogger();
        var middleware = new RetryMiddleware(new FakeLogger<RetryMiddleware>(logger));

        using var cts = new CancellationTokenSource();
        var sp = new ServiceCollection().BuildServiceProvider();
        var ctx = new ScheduledJobContext("k", sp, cts.Token)
        {
            RetryOptions = RetryTestSupport.WithDelay(TimeSpan.FromSeconds(30)),
        };

        var attempts = 0;
        ScheduledJobDelegate next = _ =>
        {
            attempts++;
            throw new InvalidOperationException("boom");
        };

        // Act — отменяем токен сразу после старта, пока middleware в Delay.
        _ = Task.Run(
            () =>
            {
                cts.CancelAfter(TimeSpan.FromMilliseconds(50));
            },
            cts.Token);

        var act = () => middleware.InvokeAsync(ctx, next);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        attempts.Should().Be(1, "после первой неудачи middleware уходит в Delay и должен быть отменён");
    }

    /// <summary>
    /// Если <see cref="ScheduledJobContext.RetryOptions"/> равен <c>null</c>,
    /// retry НЕ выполняется: первая же неудача пробрасывается вызывающему коду
    /// (контракт middleware: «без явной политики — без retry»).
    /// </summary>
    [Fact]
    public async Task InvokeAsync_RetryOptionsNull_DoesNotRetry_PropagatesException()
    {
        // Arrange
        var logger = new FakeLogger();
        var middleware = new RetryMiddleware(new FakeLogger<RetryMiddleware>(logger));
        var sp = new ServiceCollection().BuildServiceProvider();
        var ctx = new ScheduledJobContext("no-retry", sp, TestContext.Current.CancellationToken)
        {
            RetryOptions = null,
        };

        var attempts = 0;
        ScheduledJobDelegate next = _ =>
        {
            attempts++;
            throw new InvalidOperationException("boom");
        };

        // Act
        var act = () => middleware.InvokeAsync(ctx, next);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        attempts.Should().Be(1, "без RetryOptions retry не выполняется");
        logger.Entries.Should().NotContain(e => e.Level == LogLevel.Warning,
            "без RetryOptions middleware не должен логировать предупреждения о повторах");
    }

    /// <summary>
    /// <see cref="RetryOptions.MaxAttempts"/> со значением вне допустимого диапазона
    /// (ноль или отрицательное) выбрасывает <see cref="ArgumentOutOfRangeException"/>
    /// — защита от бесконечного цикла.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RetryOptions_MaxAttemptsOutOfRange_ThrowsArgumentOutOfRangeException(int invalidValue)
    {
        // Arrange / Act
        var act = () => new RetryOptions { MaxAttempts = invalidValue };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*at least 1*");
    }

    /// <summary>
    /// Отмена <see cref="CancellationToken"/> во время <c>Task.Delay</c> между попытками
    /// приводит к <see cref="OperationCanceledException"/>, и вторая попытка не выполняется.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_CancellationDuringRetryDelay_ThrowsOperationCanceledWithoutRetry()
    {
        // Arrange
        var logger = new FakeLogger();
        var middleware = new RetryMiddleware(new FakeLogger<RetryMiddleware>(logger));
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        var sp = new ServiceCollection().BuildServiceProvider();
        var ctx = new ScheduledJobContext("k", sp, cts.Token)
        {
            RetryOptions = RetryTestSupport.WithDelay(TimeSpan.FromSeconds(30)),
        };

        var attempts = 0;
        ScheduledJobDelegate next = _ =>
        {
            attempts++;
            throw new InvalidOperationException("boom");
        };

        // Act
        var act = () => middleware.InvokeAsync(ctx, next);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        attempts.Should().Be(1, "отмена во время Delay должна прервать retry до второй попытки");
    }

    /// <summary>
    /// При предварительно отменённом <see cref="CancellationToken"/> первая неудача
    /// приводит к <see cref="OperationCanceledException"/> без retry (счётчик = 1).
    /// </summary>
    [Fact]
    public async Task InvokeAsync_PreCancelledToken_ThrowsOperationCanceledImmediately()
    {
        // Arrange
        var logger = new FakeLogger();
        var middleware = new RetryMiddleware(new FakeLogger<RetryMiddleware>(logger));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var sp = new ServiceCollection().BuildServiceProvider();
        var ctx = new ScheduledJobContext("k", sp, cts.Token)
        {
            RetryOptions = RetryTestSupport.DefaultOptions(),
        };

        var attempts = 0;
        ScheduledJobDelegate next = _ =>
        {
            attempts++;
            throw new InvalidOperationException("boom");
        };

        // Act
        var act = () => middleware.InvokeAsync(ctx, next);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        attempts.Should().Be(1, "pre-cancelled CT: Task.Delay возвращает cancelled task ещё до retry");
    }

    /// <summary>
    /// Отмена <see cref="CancellationToken"/> внутри <c>next</c> после успешного
    /// выполнения не прерывает middleware и не мешает пост-обработке в pipeline.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_CancellationAfterSuccess_NextActionStillRuns()
    {
        // Arrange
        var logger = new FakeLogger();
        var retryMiddleware = new RetryMiddleware(new FakeLogger<RetryMiddleware>(logger));
        using var cts = new CancellationTokenSource();
        var sp = new ServiceCollection().BuildServiceProvider();
        var ctx = new ScheduledJobContext("k", sp, cts.Token)
        {
            RetryOptions = RetryTestSupport.DefaultOptions(),
        };

        var nextHandlerInvoked = false;
        ScheduledJobDelegate terminalAction = _ =>
        {
            cts.Cancel();
            return Task.CompletedTask;
        };

        ScheduledJobDelegate nextInPipeline = async _ =>
        {
            await retryMiddleware.InvokeAsync(ctx, terminalAction);
            nextHandlerInvoked = true;
        };

        // Act
        await nextInPipeline(ctx);

        // Assert
        nextHandlerInvoked.Should().BeTrue(
            "next handler в pipeline должен выполниться, даже если токен отменён в terminalAction");
    }

    /// <summary>
    /// При <see cref="RetryOptions.Delay"/> = <see cref="TimeSpan.Zero"/> и отменённом
    /// <see cref="CancellationToken"/> <see cref="OperationCanceledException"/> пробрасывается
    /// сразу после первой неудачи: <c>Task.Delay(0, cancelledToken)</c> возвращает cancelled task.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_DelayIsZero_CancellationImmediate()
    {
        // Arrange
        var logger = new FakeLogger();
        var middleware = new RetryMiddleware(new FakeLogger<RetryMiddleware>(logger));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var sp = new ServiceCollection().BuildServiceProvider();
        var ctx = new ScheduledJobContext("k", sp, cts.Token)
        {
            RetryOptions = new RetryOptions
            {
                MaxAttempts = 5,
                Delay = TimeSpan.Zero,
            },
        };

        var attempts = 0;
        ScheduledJobDelegate next = _ =>
        {
            attempts++;
            throw new InvalidOperationException("boom");
        };

        // Act
        var act = () => middleware.InvokeAsync(ctx, next);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        attempts.Should().Be(1, "Delay = Zero + cancelled CT: Task.Delay возвращает cancelled task, retry не выполняется");
    }

    /// <summary>
    /// Многократный вызов <c>Cancel</c> на <see cref="CancellationTokenSource"/> идемпотентен:
    /// middleware выбрасывает ровно одно <see cref="OperationCanceledException"/> и не делает
    /// лишних попыток.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_MultipleCancellations_DoesNotDoubleThrow()
    {
        // Arrange
        var logger = new FakeLogger();
        var middleware = new RetryMiddleware(new FakeLogger<RetryMiddleware>(logger));
        using var cts = new CancellationTokenSource();
        var sp = new ServiceCollection().BuildServiceProvider();
        var ctx = new ScheduledJobContext("k", sp, cts.Token)
        {
            RetryOptions = RetryTestSupport.WithDelay(TimeSpan.FromSeconds(30)),
        };

        var attempts = 0;
        ScheduledJobDelegate next = _ =>
        {
            attempts++;
            throw new InvalidOperationException("boom");
        };

        // Многократная отмена токена — должна быть идемпотентной
        cts.Cancel();
        cts.Cancel();
        cts.Cancel();

        // Act
        var act = () => middleware.InvokeAsync(ctx, next);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        attempts.Should().Be(1, "многократный Cancel CTS идемпотентен — одна попытка, одно исключение");
    }

    /// <summary>
    /// Middleware реагирует только на <see cref="ScheduledJobContext.CancellationToken"/>;
    /// отмена постороннего <see cref="CancellationToken"/> не учитывается.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_CancellationViaDifferentToken_OnlyRespondsToOwnToken()
    {
        // Arrange
        var logger = new FakeLogger();
        var middleware = new RetryMiddleware(new FakeLogger<RetryMiddleware>(logger));
        using var unrelatedCts = new CancellationTokenSource();
        unrelatedCts.Cancel();

        var sp = new ServiceCollection().BuildServiceProvider();
        var ctx = new ScheduledJobContext("k", sp, TestContext.Current.CancellationToken)
        {
            RetryOptions = RetryTestSupport.DefaultOptions(),
        };

        var attempts = 0;
        ScheduledJobDelegate next = _ =>
        {
            attempts++;
            throw new InvalidOperationException("boom");
        };

        // Act
        var act = () => middleware.InvokeAsync(ctx, next);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        attempts.Should().Be(RetryTestSupport.DefaultOptions().MaxAttempts,
            "retry выполняется до MaxAttempts, потому что context.CancellationToken не отменён");
    }
}
