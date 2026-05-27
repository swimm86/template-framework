using System.Net;
using Shared.Application.Core.Batch.Http.RetryPolicy;
using Shared.Application.Core.Batch.Http.RetryPolicy.Extensions;
using Shared.Application.Core.Batch.Http.RetryPolicy.Models;

namespace Shared.Application.Core.Tests;

/// <summary>
/// Тесты <see cref="RetryConfiguration.Validate"/> и выполнения повторов в <see cref="DefaultHttpBatchRetryPolicy"/>.
/// </summary>
public sealed class PageableBatchRetryOptionsTests
{
    /// <summary>
    /// Некорректные опции и ожидаемое имя параметра в исключении.
    /// </summary>
    public static TheoryData<RetryConfiguration, string> InvalidValidateCases { get; } = new()
    {
        {
            new RetryConfiguration
            {
                Backoff = new BackoffConfiguration
                {
                    MaxAttempts = 0,
                },
            },
            nameof(BackoffConfiguration.MaxAttempts)
        },
        {
            new RetryConfiguration
            {
                Backoff = new BackoffConfiguration
                {
                    MaxAttempts = 2,
                    InitialDelay = TimeSpan.FromMilliseconds(-1),
                },
            },
            nameof(BackoffConfiguration.InitialDelay)
        },
    };

    /// <summary>
    /// Допустимые значения <see cref="BackoffConfiguration.MaxAttempts"/> для успешного вызова <see cref="RetryConfiguration.Validate"/>.
    /// </summary>
    public static TheoryData<int> ValidMaxAttempts { get; } = [1, 3];

    /// <summary>
    /// Сценарии «транзиентная ошибка на первых попытках, затем успех»: фабрика исключения по номеру попытки, лимит попыток, ожидаемое число вызовов делегата, ожидаемый результат.
    /// </summary>
    public static TheoryData<Func<int, Exception?>, int, int, int> TransientRetriesSucceedCases { get; } =
        BuildTransientRetriesSucceedCases();

    private static TheoryData<Func<int, Exception?>, int, int, int> BuildTransientRetriesSucceedCases()
    {
        var data = new TheoryData<Func<int, Exception?>, int, int, int>
        {
            // failCount before success, maxAttempts, expectedTotalCalls, expectedResult
            { attempt => attempt < 2 ? new IOException("n") : null, 3, 2, 7 },
            { attempt => attempt < 3 ? new TimeoutException() : null, 4, 3, 0 },
            { attempt => attempt < 2 ? new TaskCanceledException("deadline") : null, 2, 2, 1 },
            { attempt => attempt < 2 ? new HttpRequestException("outer") : null, 2, 2, 5 },
            {
                attempt => attempt < 2
                    ? new InvalidOperationException("wrap", new HttpRequestException("inner"))
                    : null,
                2, 2, 99
            },
        };

        return data;
    }

    /// <summary>
    /// <see cref="RetryConfiguration.Validate"/> выбрасывает <see cref="ArgumentOutOfRangeException"/> с ожидаемым именем параметра.
    /// </summary>
    /// <param name="options">Невалидная конфигурация.</param>
    /// <param name="expectedParamName">Имя свойства, указанное в исключении.</param>
    [Theory]
    [MemberData(nameof(InvalidValidateCases))]
    public void Validate_WhenPropertyOutOfRange_ThrowsWithExpectedParamName(
        RetryConfiguration options,
        string expectedParamName)
    {
        // Arrange
        // Act
        var act = options.Validate;

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .Where(ex => ex.ParamName == expectedParamName);
    }

    /// <summary>
    /// При допустимых <paramref name="maxAttempts"/> валидация не выбрасывает (в т.ч. при <c>MaxAttempts == 1</c> и отрицательной задержке — правило не применяется).
    /// </summary>
    /// <param name="maxAttempts">1 или больше.</param>
    [Theory]
    [MemberData(nameof(ValidMaxAttempts))]
    public void Validate_WhenMaxAttemptsValid_DoesNotThrow(int maxAttempts)
    {
        // Arrange
        var options = new RetryConfiguration
        {
            Backoff = new BackoffConfiguration
            {
                MaxAttempts = maxAttempts,
                InitialDelay = maxAttempts > 1 ? TimeSpan.Zero : TimeSpan.FromSeconds(-1),
            },
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Нулевая начальная задержка при включённых повторах (<see cref="BackoffConfiguration.MaxAttempts"/> &gt; 1) допустима.
    /// </summary>
    [Fact]
    public void Validate_WhenMaxAttemptsGreaterThanOne_AndZeroDelay_DoesNotThrow()
    {
        // Arrange
        var options = new RetryConfiguration
        {
            Backoff = new BackoffConfiguration
            {
                MaxAttempts = 3,
                InitialDelay = TimeSpan.Zero,
            },
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// При <see cref="BackoffConfiguration.MaxAttempts"/> равном 1 повторы отключены: делегат вызывается ровно один раз без задержек retry.
    /// </summary>
    [Fact]
    public async Task InvokePageRequestAsync_WhenMaxAttemptsOne_InvokesOnce()
    {
        // Arrange
        var calls = 0;
        var options = new RetryConfiguration
        {
            Backoff = new BackoffConfiguration
            {
                MaxAttempts = 1,
            },
        };

        var policy = new DefaultHttpBatchRetryPolicy(options);

        // Act
        var result = await policy.ExecuteAsync(
            _ =>
            {
                calls++;
                return Task.FromResult(42);
            },
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(42);
        calls.Should().Be(1);
    }

    /// <summary>
    /// После серии транзиентных сбоев (см. <see cref="TransientRetriesSucceedCases"/>) успешная попытка возвращает ожидаемый результат; число вызовов совпадает с ожиданием.
    /// </summary>
    /// <param name="failOrNullOnAttempt">Для номера попытки возвращает исключение или <see langword="null"/> (успех).</param>
    /// <param name="maxAttempts">Максимум попыток страницы.</param>
    /// <param name="expectedTotalCalls">Сколько раз должен вызываться делегат.</param>
    /// <param name="expectedResult">Возвращаемое значение при успехе.</param>
    [Theory]
    [MemberData(nameof(TransientRetriesSucceedCases))]
    public async Task InvokePageRequestAsync_WhenTransientFailsThenSucceeds_ReturnsExpected(
        Func<int, Exception?> failOrNullOnAttempt,
        int maxAttempts,
        int expectedTotalCalls,
        int expectedResult)
    {
        // Arrange
        var attempt = 0;
        var options = new RetryConfiguration
        {
            Backoff = new BackoffConfiguration
            {
                MaxAttempts = maxAttempts,
                InitialDelay = TimeSpan.FromMilliseconds(1),
                MaxDelay = TimeSpan.FromMilliseconds(5),
            },
        };

        var policy = new DefaultHttpBatchRetryPolicy(options);

        // Act
        var result = await policy.ExecuteAsync(
            _ =>
            {
                var a = Interlocked.Increment(ref attempt);
                var fail = failOrNullOnAttempt(a);
                if (fail is not null)
                {
                    throw fail;
                }

                return Task.FromResult(expectedResult);
            },
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedResult);
        attempt.Should().Be(expectedTotalCalls);
    }

    /// <summary>
    /// Уже отменённый внешний токен: первый вызов завершается <see cref="TaskCanceledException"/> без дополнительных попыток retry.
    /// </summary>
    [Fact]
    public async Task InvokePageRequestAsync_WhenCanceled_ThrowsTaskCanceledWithoutRetries()
    {
        // Arrange
        var attempt = 0;
        var options = new RetryConfiguration
        {
            Backoff = new BackoffConfiguration
            {
                MaxAttempts = 3,
                InitialDelay = TimeSpan.FromMilliseconds(1),
                MaxDelay = TimeSpan.FromMilliseconds(5),
            },
        };

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var policy = new DefaultHttpBatchRetryPolicy(options);

        // Act
        var act = () => policy.ExecuteAsync(
                _ =>
                {
                    Interlocked.Increment(ref attempt);
                    throw new TaskCanceledException();
                },
                cts.Token);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
        attempt.Should().Be(1);
    }

    /// <summary>
    /// Нетранзиентное исключение (<see cref="InvalidOperationException"/>) не приводит к повтору; одна попытка.
    /// </summary>
    [Fact]
    public async Task InvokePageRequestAsync_WhenNotTransient_DoesNotRetry()
    {
        // Arrange
        var attempt = 0;
        var options = new RetryConfiguration
        {
            Backoff = new BackoffConfiguration
            {
                MaxAttempts = 5,
                InitialDelay = TimeSpan.FromMilliseconds(1),
                MaxDelay = TimeSpan.FromMilliseconds(5),
            },
        };

        var policy = new DefaultHttpBatchRetryPolicy(options);

        // Act
        var act = () => policy.ExecuteAsync(
                _ =>
                {
                    Interlocked.Increment(ref attempt);
                    throw new InvalidOperationException("no retry");
                },
                TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        attempt.Should().Be(1);
    }

    /// <summary>
    /// HTTP 404 не считается транзиентным; повторов нет.
    /// </summary>
    [Fact]
    public async Task InvokePageRequestAsync_WhenHttp404_DoesNotRetry()
    {
        // Arrange
        var attempt = 0;
        var options = new RetryConfiguration
        {
            Backoff = new BackoffConfiguration
            {
                MaxAttempts = 3,
                InitialDelay = TimeSpan.FromMilliseconds(1),
                MaxDelay = TimeSpan.FromMilliseconds(5),
            },
        };

        var policy = new DefaultHttpBatchRetryPolicy(options);

        // Act
        var act = () => policy.ExecuteAsync(
                _ =>
                {
                    Interlocked.Increment(ref attempt);
                    throw new HttpRequestException("nf", null, HttpStatusCode.NotFound);
                },
                TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
        attempt.Should().Be(1);
    }

    /// <summary>
    /// HTTP 503 транзиентен: после одной неудачной попытки следующая успешна, результат возвращается.
    /// </summary>
    [Fact]
    public async Task InvokePageRequestAsync_WhenHttp503_ThenSucceeds_RetriesOnce()
    {
        // Arrange
        var attempt = 0;
        var options = new RetryConfiguration
        {
            Backoff = new BackoffConfiguration
            {
                MaxAttempts = 3,
                InitialDelay = TimeSpan.FromMilliseconds(1),
                MaxDelay = TimeSpan.FromMilliseconds(5),
            },
        };

        var policy = new DefaultHttpBatchRetryPolicy(options);

        // Act
        var result = await policy.ExecuteAsync(
            _ =>
            {
                var a = Interlocked.Increment(ref attempt);
                if (a < 2)
                {
                    throw new HttpRequestException(
                        "tmp",
                        null,
                        HttpStatusCode.ServiceUnavailable);
                }

                return Task.FromResult(42);
            },
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(42);
        attempt.Should().Be(2);
    }

    /// <summary>
    /// Расчёт паузы между попытками: экспоненциальный рост от <paramref name="initialMs"/> и ограничение сверху <paramref name="capMs"/>.
    /// </summary>
    /// <param name="failedAttempt">Номер неудачной попытки (1-based сценарий backoff).</param>
    /// <param name="initialMs">Начальная задержка в миллисекундах.</param>
    /// <param name="capMs">Потолок в миллисекундах или <see langword="null"/> без потолка.</param>
    /// <param name="expectedMs">Ожидаемая задержка в миллисекундах.</param>
    [Theory]
    [InlineData(1, 100, null, 100)]
    [InlineData(2, 100, null, 200)]
    [InlineData(3, 100, null, 400)]
    [InlineData(3, 100, 250, 250)]
    public void ComputeBackoff_ScalesExponentially_AndCapsAtMaxDelay(
        int failedAttempt,
        int initialMs,
        int? capMs,
        int expectedMs)
    {
        // Arrange
        TimeSpan? cap = capMs.HasValue ? TimeSpan.FromMilliseconds(capMs.Value) : null;

        // Act
        var delay = HttpBatchRetryHelper.ComputeBackoff(
            failedAttempt,
            TimeSpan.FromMilliseconds(initialMs),
            cap);

        // Assert
        delay.Should().Be(TimeSpan.FromMilliseconds(expectedMs));
    }

    /// <summary>
    /// <see cref="OperationCanceledException"/> без признаков транзиентности по политике retry не повторяется.
    /// </summary>
    [Fact]
    public async Task InvokePageRequestAsync_WhenOperationCanceled_NotRetried()
    {
        // Arrange
        var attempt = 0;
        var options = new RetryConfiguration
        {
            Backoff = new BackoffConfiguration
            {
                MaxAttempts = 4,
                InitialDelay = TimeSpan.FromMilliseconds(1),
                MaxDelay = TimeSpan.FromMilliseconds(5),
            },
        };

        var policy = new DefaultHttpBatchRetryPolicy(options);

        // Act
        var act = () => policy.ExecuteAsync(
                _ =>
                {
                    Interlocked.Increment(ref attempt);
                    throw new OperationCanceledException();
                },
                TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        attempt.Should().Be(1);
    }

    /// <summary>
    /// Отрицательная <see cref="BackoffConfiguration.InitialDelay"/> при
    /// <c>MaxAttempts &gt; 1</c> отклоняется в конструкторе <see cref="DefaultHttpBatchRetryPolicy"/> (<see cref="RetryConfiguration.Validate"/>).
    /// </summary>
    [Fact]
    public void DefaultPageableBatchRetryPolicy_WhenInitialDelayNegative_ThrowsFromCtor()
    {
        // Arrange
        var options = new RetryConfiguration
        {
            Backoff = new BackoffConfiguration
            {
                MaxAttempts = 2,
                InitialDelay = TimeSpan.FromMilliseconds(-1),
                MaxDelay = TimeSpan.FromMilliseconds(5),
            },
        };

        // Act
        var act = () => _ = new DefaultHttpBatchRetryPolicy(options);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .Where(ex => ex.ParamName == nameof(BackoffConfiguration.InitialDelay));
    }

    /// <summary>
    /// Отрицательный <see cref="BackoffConfiguration.MaxDelay"/> отклоняется методом <see cref="RetryConfiguration.Validate"/>.
    /// </summary>
    [Fact]
    public void Validate_WhenMaxDelayNegative_ThrowsWithParamMaxDelay()
    {
        // Arrange
        var options = new RetryConfiguration
        {
            Backoff = new BackoffConfiguration
            {
                MaxAttempts = 2,
                InitialDelay = TimeSpan.Zero,
                MaxDelay = TimeSpan.FromSeconds(-1),
            },
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .Where(ex => ex.ParamName == nameof(BackoffConfiguration.MaxDelay));
    }

    /// <summary>
    /// Подсказки Retry-After в <see cref="Exception.Data"/> по цепочке inner объединяются как максимум положительной паузы.
    /// </summary>
    [Fact]
    public void TryGetRetryAfterHintFromExceptionChain_ReturnsMaxAcrossExceptions()
    {
        // Arrange
        var inner = new HttpRequestException("inner", null, HttpStatusCode.TooManyRequests)
        {
            Data =
            {
                [TransientConfiguration.RetryAfterExceptionDataKey] = 10,
            }
        };

        var outer = new InvalidOperationException("outer", inner)
        {
            Data =
            {
                [TransientConfiguration.RetryAfterExceptionDataKey] = 30,
            }
        };

        // Act
        var hint = HttpBatchRetryHelper.TryGetRetryAfterMaxFromChain(outer);

        // Assert
        hint.Should().Be(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Дополнительное правило транзиентности позволяет повторить запрос при исключении вне встроенного списка типов.
    /// </summary>
    [Fact]
    public async Task InvokePageRequestAsync_WhenAdditionalTransientPredicate_RetriesOnce()
    {
        // Arrange
        var attempt = 0;
        var options = new RetryConfiguration
        {
            Backoff = new BackoffConfiguration
            {
                MaxAttempts = 3,
                InitialDelay = TimeSpan.FromMilliseconds(1),
                MaxDelay = TimeSpan.FromMilliseconds(10),
                UseBackoffJitter = false,
            },
            Transient = new TransientConfiguration
            {
                IsAdditionalTransientException = ex =>
                    ex is InvalidOperationException { Message: "transient-custom" },
            },
        };

        var policy = new DefaultHttpBatchRetryPolicy(options);

        // Act
        var result = await policy.ExecuteAsync(
            _ =>
            {
                var a = Interlocked.Increment(ref attempt);
                if (a < 2)
                {
                    throw new InvalidOperationException("transient-custom");
                }

                return Task.FromResult(77);
            },
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(77);
        attempt.Should().Be(2);
    }

    /// <summary>
    /// Парсинг значения ключа <see cref="TransientConfiguration.RetryAfterExceptionDataKey"/> для одиночного исключения.
    /// </summary>
    /// <param name="dataValue">Значение в <see cref="Exception.Data"/>.</param>
    /// <param name="expectedSeconds">Ожидаемая длительность в секундах.</param>
    [Theory]
    [InlineData(120, 120)]
    [InlineData("60", 60)]
    public void ParseRetryAfterFromExceptionDataSingle_ParsesPositiveValues(
        object dataValue,
        int expectedSeconds)
    {
        // Arrange
        var ex = new HttpRequestException("x", null, HttpStatusCode.TooManyRequests)
        {
            Data =
            {
                [TransientConfiguration.RetryAfterExceptionDataKey] = dataValue,
            }
        };

        // Act
        var hint = HttpBatchRetryHelper.TryGetRetryAfterMaxFromChain(ex);

        // Assert
        hint.Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }
}
