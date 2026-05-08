using Shared.Application.Core.Batch.Http.Extensions;
using Shared.Application.Core.Batch.Http.RetryPolicy;
using Shared.Application.Core.Batch.Http.RetryPolicy.Models;
using Shared.Application.Core.Dto.Responses;
using Shared.Application.Core.Tests.Support;
using static Shared.Application.Core.Tests.Support.PageableHttpBatchTestSupport;
using Shared.Common.Extensions;

namespace Shared.Application.Core.Tests;

/// <summary>
/// Юнит-тесты <see cref="HttpRequestExtensions"/>: постраничный обход, повторы, отмена и валидация аргументов.
/// </summary>
/// <remarks>
/// Проверяется интеграция с <see cref="Common.Batch.BatchHelper"/>:
/// число HTTP-вызовов, отсутствие повторов при ошибках пользовательского <c>processFunc</c>, согласованность
/// <see cref="Shared.Application.Core.Dto.Requests.PageableRequest.PageNumber"/> с телом ответа.
/// </remarks>
public sealed class HttpRequestExtensionsTests
{
    /// <summary>
    /// Кейсы «успешного» API: использовать ли перегрузку с <see cref="CancellationToken"/>, число страниц в ответе, ожидаемые номера страниц у потребителя.
    /// </summary>
    public static TheoryData<bool, int, int[]> HealthyBatchProcessCases { get; } = BuildHealthyBatchProcessCases();

    /// <summary>
    /// Кейсы «успешного» API: валидация номера страницы.
    /// </summary>
    public static TheoryData<int> BatchSelectPagesAsyncRequestPageNumberValidationCases { get; } =
        new(Enumerable.Range(-1, 3).ToArray());

    private static TheoryData<bool, int, int[]> BuildHealthyBatchProcessCases()
    {
        var data = new TheoryData<bool, int, int[]>();
        foreach (var useCt in new[] { true, false })
        {
            data.Add(useCt, 1, [1]);
            data.Add(useCt, 3, [1, 2, 3]);
        }

        return data;
    }

    /// <summary>
    /// Недопустимые значения размера страницы (батча) для проверки <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    public static TheoryData<int> InvalidBatchSizes { get; } = [0, -1];

    /// <summary>
    /// При корректных ответах API обе перегрузки <c>BatchProcessAsync</c> (с токеном и без) обрабатывают все страницы по порядку.
    /// </summary>
    /// <param name="useCancellationTokenOverload">Если <see langword="true"/> — используется делегат с <see cref="CancellationToken"/>.</param>
    /// <param name="totalPages">Значение <see cref="PageableResponse{T}.TotalPages"/> в заглушке.</param>
    /// <param name="expectedPages">Ожидаемая последовательность <see cref="PageableResponse{T}.PageNumber"/> в колбэке обработки.</param>
    [Theory]
    [MemberData(nameof(HealthyBatchProcessCases))]
    public async Task BatchProcessAsync_WhenHealthy_YieldsEveryPageNumber(
        bool useCancellationTokenOverload,
        int totalPages,
        int[] expectedPages)
    {
        const int batchSize = 10;
        var request = new TestPageableRequest { PageSize = batchSize };
        var processed = new List<int>();

        if (useCancellationTokenOverload)
        {
            await ExecuteBatchProcessAsync(
                BuildRequestFuncWithToken(totalPages),
                request,
                processFunc: r =>
                {
                    processed.Add(r.PageNumber);
                    return Task.CompletedTask;
                });
        }
        else
        {
            await ExecuteBatchProcessAsync(
                BuildRequestFunc(totalPages),
                request,
                processFunc: r =>
                {
                    processed.Add(r.PageNumber);
                    return Task.CompletedTask;
                });
        }

        Assert.Equal(expectedPages, processed);
    }

    /// <summary>
    /// Поток <c>BatchSelectPagesAsync</c> без CT содержит те же номера страниц, что и синхронная обработка через <c>ForEachAsync</c>.
    /// </summary>
    /// <param name="_">Зарезервировано под симметрию с <see cref="HealthyBatchProcessCases"/> (перегрузка с CT здесь не используется).</param>
    /// <param name="totalPages">Число страниц в заглушке.</param>
    /// <param name="expectedPages">Ожидаемые номера страниц в потребителе.</param>
    [Theory]
    [MemberData(nameof(HealthyBatchProcessCases))]
    public async Task BatchSelectPagesAsync_WhenHealthyViaForEach_ContainsEveryPage(
        bool _,
        int totalPages,
        int[] expectedPages)
    {
        const int batchSize = 10;
        var request = new TestPageableRequest { PageSize = batchSize };
        var processed = new List<int>();

        await ExecuteBatchSelectPagesAsync(BuildRequestFunc(totalPages), request)
            .ForEachAsync(
                page =>
                {
                    processed.Add(page.PageNumber);
                    return Task.CompletedTask;
                },
                TestContext.Current.CancellationToken);

        Assert.Equal(expectedPages, processed);
    }

    /// <summary>
    /// Транзиентный <see cref="HttpRequestException"/> на первой странице приводит к повторам и успешному завершению; число попыток ограничено настройками retry.
    /// </summary>
    [Fact]
    public async Task BatchProcessAsync_WhenTransientOnFirstPage_RetriesUntilSuccess()
    {
        const int batchSize = 10;
        var request = new TestPageableRequest { PageSize = batchSize };
        var processed = new List<int>();
        var attempt = 0;

        Func<TestPageableRequest, CancellationToken, Task<PageableResponse<IReadOnlyCollection<int>>>> requestFunc =
            (req, _) =>
            {
                if (Interlocked.Increment(ref attempt) < 3)
                {
                    throw new HttpRequestException("transient");
                }

                var payload = new[] { req.PageNumber };
                return Task.FromResult(new PageableResponse<IReadOnlyCollection<int>>(1, req.PageNumber, payload));
            };

        await requestFunc
            .BatchProcessAsync<
                TestPageableRequest,
                PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
                request,
                processFunc: r =>
                {
                    processed.Add(r.PageNumber);
                    return Task.CompletedTask;
                },
                pageRetryPolicy: FastRetryPolicy(4),
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(new[] { 1 }, processed);
        Assert.Equal(3, attempt);
    }

    /// <summary>
    /// При постоянном транзиентном сбое после исчерпания <see cref="BackoffConfiguration.MaxAttempts"/> пробрасывается последнее <see cref="HttpRequestException"/>.
    /// </summary>
    [Fact]
    public async Task BatchProcessAsync_WhenAlwaysTransient_ThrowsAfterLastAttempt()
    {
        const int batchSize = 10;
        var request = new TestPageableRequest {PageSize = batchSize };
        Func<TestPageableRequest, CancellationToken, Task<PageableResponse<IReadOnlyCollection<int>>>> requestFunc =
            (_, _) => throw new HttpRequestException("fail");

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            requestFunc.BatchProcessAsync<
                TestPageableRequest,
                PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
                request,
                processFunc: null,
                pageRetryPolicy: new DefaultHttpBatchRetryPolicy(
                    new RetryConfiguration
                    {
                        Backoff = new BackoffConfiguration
                        {
                            MaxAttempts = 2,
                            InitialDelay = TimeSpan.FromMilliseconds(1),
                            MaxDelay = TimeSpan.FromMilliseconds(5),
                        },
                    }),
                cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// В каждый вызов делегата запроса передаются ожидаемые <see cref="Shared.Application.Core.Dto.Requests.PageableRequest.PageNumber"/> и размер страницы (равный переданному batch size).
    /// </summary>
    [Fact]
    public async Task BatchSelectPagesAsync_WithToken_PropagatesPageAndBatchSizeToRequest()
    {
        const int batchSize = 7;
        var request = new TestPageableRequest { PageSize = batchSize };
        var snapshots = new List<(int PageNumber, int PageSize)>();

        await BuildRequestFuncWithToken(totalPages: 3)
            .BatchSelectPagesAsync<
                TestPageableRequest,
                PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
                request,
                pageRetryPolicy: null,
                cancellationToken: TestContext.Current.CancellationToken)
            .ForEachAsync(
                page =>
                {
                    snapshots.Add((page.PageNumber, request.PageSize));
                    return Task.CompletedTask;
                },
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal([(1, batchSize), (2, batchSize), (3, batchSize)], snapshots);
    }

    /// <summary>
    /// Отсутствие пользовательского <c>processFunc</c> не ломает обход: страницы запрашиваются, исключений нет.
    /// </summary>
    [Fact]
    public async Task BatchProcessAsync_WhenProcessFuncIsNull_CompletesWithoutError()
    {
        const int batchSize = 10;
        var request = new TestPageableRequest { PageSize = batchSize };
        await BuildRequestFuncWithToken(totalPages: 2)
            .BatchProcessAsync<
                TestPageableRequest,
                PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
                request,
                processFunc: null,
                pageRetryPolicy: null,
                cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Перегрузка с токеном: <see langword="null"/> в качестве модели запроса даёт <see cref="ArgumentNullException"/> при создании перечисления.
    /// </summary>
    [Fact]
    public void BatchSelectPagesAsync_WithToken_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        var requestFunc = BuildRequestFuncWithToken(totalPages: 1);

        Assert.Throws<ArgumentNullException>(() =>
            _ = requestFunc.BatchSelectPagesAsync<
                TestPageableRequest,
                PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
                request: null!,
                pageRetryPolicy: null,
                cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Неположительный <paramref name="batchSize"/> приводит к <see cref="ArgumentOutOfRangeException"/> до выполнения HTTP.
    /// </summary>
    /// <param name="batchSize">Ноль или отрицательное значение размера страницы.</param>
    [Theory]
    [MemberData(nameof(InvalidBatchSizes))]
    public void BatchSelectPagesAsync_WithToken_WhenBatchSizeNotPositive_ThrowsArgumentOutOfRangeException(
        int batchSize)
    {
        var request = new TestPageableRequest { PageSize = batchSize };
        var requestFunc = BuildRequestFuncWithToken(totalPages: 1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _ = requestFunc.BatchSelectPagesAsync<
                TestPageableRequest,
                PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
                request,
                pageRetryPolicy: null,
                cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Некорректные <see cref="RetryConfiguration"/> (например, <c>MaxAttempts</c> вне допустимого диапазона) отклоняются до первого сетевого вызова.
    /// </summary>
    [Fact]
    public async Task BatchProcessAsync_WhenRetryOptionsInvalid_ThrowsBeforeHttp()
    {
        const int batchSize = 10;
        var request = new TestPageableRequest { PageSize = batchSize };
        var requestFunc = BuildRequestFuncWithToken(totalPages: 1);
        var retry = new RetryConfiguration
        {
            Backoff = new BackoffConfiguration
            {
                MaxAttempts = 0,
            },
        };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            requestFunc.BatchProcessAsync<
                TestPageableRequest,
                PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
                request,
                processFunc: null,
                pageRetryPolicy: new DefaultHttpBatchRetryPolicy(retry),
                cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Транзиентный сбой только на второй странице обрабатывается retry; в потоке остаются обе страницы, вторая попытка второй страницы успешна.
    /// </summary>
    [Fact]
    public async Task BatchSelectPagesAsync_WhenSecondPageTransientOnce_ReturnsBothPages()
    {
        const int batchSize = 10;
        var request = new TestPageableRequest { PageSize = batchSize };
        var page2Attempts = 0;

        Func<TestPageableRequest, CancellationToken, Task<PageableResponse<IReadOnlyCollection<int>>>> requestFunc =
            (req, _) =>
            {
                if (req.PageNumber == 1)
                {
                    return Task.FromResult(new PageableResponse<IReadOnlyCollection<int>>(2, 1, [1]));
                }

                if (Interlocked.Increment(ref page2Attempts) < 2)
                {
                    throw new HttpRequestException("transient on page 2");
                }

                return Task.FromResult(new PageableResponse<IReadOnlyCollection<int>>(2, 2, [2]));
            };

        var pages = await requestFunc
            .BatchSelectPagesAsync<
                TestPageableRequest,
                PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
                request,
                pageRetryPolicy: FastRetryPolicy(maxAttempts: 3),
                cancellationToken: TestContext.Current.CancellationToken)
            .ToPageNumberListAsync();

        Assert.Equal(new[] { 1, 2 }, pages);
        Assert.Equal(2, page2Attempts);
    }

    /// <summary>
    /// Перегрузка <c>BatchProcessAsync</c> без CT в делегате: <see langword="null"/> делегат запроса — <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public async Task BatchProcessAsync_WhenNoCtOverload_AndRequestFuncNull_ThrowsWhenRun()
    {
        const int batchSize = 10;
        var request = new TestPageableRequest { PageSize = batchSize };
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ((Func<TestPageableRequest, Task<PageableResponse<IReadOnlyCollection<int>>>>)null!)
            .BatchProcessAsync<
                TestPageableRequest,
                PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
                request,
                processFunc: null,
                pageRetryPolicy: null,
                cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Перегрузка <c>BatchSelectPagesAsync</c> без CT: <see langword="null"/> делегат запроса — <see cref="ArgumentNullException"/> при перечислении.
    /// </summary>
    [Fact]
    public void BatchSelectPagesAsync_WhenNoCtOverload_AndRequestFuncNull_ThrowsArgumentNull()
    {
        const int batchSize = 10;
        var request = new TestPageableRequest { PageSize = batchSize };

        Assert.Throws<ArgumentNullException>(() =>
            _ = ((Func<TestPageableRequest, Task<PageableResponse<IReadOnlyCollection<int>>>>)null!)
                .BatchSelectPagesAsync<
                    TestPageableRequest,
                    PageableResponse<IReadOnlyCollection<int>>,
                    IReadOnlyCollection<int>>(
                    request,
                    pageRetryPolicy: null,
                    cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Перегрузка с CT: <see langword="null"/> делегат запроса — <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void BatchSelectPagesAsync_WithToken_WhenRequestFuncNull_ThrowsArgumentNull()
    {
        const int batchSize = 10;
        var request = new TestPageableRequest { PageSize = batchSize };
        Assert.Throws<ArgumentNullException>(() =>
            _ = ((Func<TestPageableRequest, CancellationToken, Task<PageableResponse<IReadOnlyCollection<int>>>>)null!)
                .BatchSelectPagesAsync<
                    TestPageableRequest,
                    PageableResponse<IReadOnlyCollection<int>>,
                    IReadOnlyCollection<int>>(
                    request,
                    pageRetryPolicy: null,
                    cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Если сервер сообщает <c>TotalPages == 0</c>, перечисление не возвращает элементов (нет страниц для обхода).
    /// </summary>
    [Fact]
    public async Task BatchSelectPagesAsync_WhenServerReportsZeroTotal_YieldsNothing()
    {
        const int batchSize = 10;
        var request = new TestPageableRequest { PageSize = batchSize };
        Func<TestPageableRequest, CancellationToken, Task<PageableResponse<IReadOnlyCollection<int>>>> requestFunc =
            (req, _) =>
                Task.FromResult(new PageableResponse<IReadOnlyCollection<int>>(0, req.PageNumber, []));

        var pages = await requestFunc
            .BatchSelectPagesAsync<
                TestPageableRequest,
                PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
                request,
                pageRetryPolicy: null,
                cancellationToken: TestContext.Current.CancellationToken)
            .ToPageNumberListAsync();

        Assert.Empty(pages);
    }

    /// <summary>
    /// Граница по метаданным: при <c>TotalPages == 1</c> запрашивается только первая страница, даже если payload мог бы подразумевать продолжение.
    /// </summary>
    [Fact]
    public async Task BatchSelectPagesAsync_WhenTotalIsOne_StopsAfterOnePageEvenIfMoreDataExists()
    {
        const int batchSize = 10;
        var request = new TestPageableRequest { PageSize = batchSize };
        Func<TestPageableRequest, CancellationToken, Task<PageableResponse<IReadOnlyCollection<int>>>> requestFunc =
            (req, _) =>
            {
                var payload = new[] { req.PageNumber };
                return Task.FromResult(new PageableResponse<IReadOnlyCollection<int>>(1, req.PageNumber, payload));
            };

        var pages = await requestFunc
            .BatchSelectPagesAsync<
                TestPageableRequest,
                PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
                request,
                pageRetryPolicy: null,
                cancellationToken: TestContext.Current.CancellationToken)
            .ToPageNumberListAsync();

        Assert.Equal(new[] { 1 }, pages);
    }

    /// <summary>
    /// Исключение из пользовательского <c>processFunc</c> не считается транзиентным HTTP-сбоем: повторных запросов страниц не выполняется.
    /// </summary>
    [Fact]
    public async Task BatchProcessAsync_WhenProcessFuncThrows_DoesNotRetryHttp()
    {
        const int batchSize = 10;
        var request = new TestPageableRequest { PageSize = batchSize };
        var httpCalls = 0;
        var requestFunc =
            BuildCountingRequestFuncWithToken(totalPages: 2, onCall: () => Interlocked.Increment(ref httpCalls));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            requestFunc.BatchProcessAsync<
                TestPageableRequest,
                PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
                request,
                processFunc: r =>
                    r.PageNumber == 2
                        ? throw new InvalidOperationException("process failed")
                        : Task.CompletedTask,
                pageRetryPolicy: null,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(2, httpCalls);
    }

    /// <summary>
    /// При <see cref="BackoffConfiguration.MaxAttempts"/> равном единице транзиентная ошибка приводит к одному вызову делегата и пробросу исключения без паузы между попытками.
    /// </summary>
    [Fact]
    public async Task BatchProcessAsync_WhenMaxAttemptsIsOne_TransientFailsOnFirstCall()
    {
        const int batchSize = 10;
        var request = new TestPageableRequest { PageSize = batchSize };
        var calls = 0;
        Func<TestPageableRequest, CancellationToken, Task<PageableResponse<IReadOnlyCollection<int>>>> requestFunc =
            (_, _) =>
            {
                Interlocked.Increment(ref calls);
                throw new HttpRequestException("transient");
            };

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            requestFunc.BatchProcessAsync<
                TestPageableRequest,
                PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
                request,
                processFunc: null,
                pageRetryPolicy: new DefaultHttpBatchRetryPolicy(
                    new RetryConfiguration
                    {
                        Backoff = new BackoffConfiguration
                        {
                            MaxAttempts = 1,
                        },
                    }),
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(1, calls);
    }

    /// <summary>
    /// Транзиентный сбой при запросе последней страницы в сценарии <c>BatchProcessAsync</c> преодолевается retry; обе страницы попадают в обработчик.
    /// </summary>
    [Fact]
    public async Task BatchProcessAsync_WhenLastPageTransientOnce_CompletesBothPages()
    {
        const int batchSize = 10;
        var request = new TestPageableRequest { PageSize = batchSize };
        var page2Calls = 0;

        Func<TestPageableRequest, CancellationToken, Task<PageableResponse<IReadOnlyCollection<int>>>> requestFunc =
            (req, _) =>
            {
                if (req.PageNumber == 1)
                {
                    return Task.FromResult(new PageableResponse<IReadOnlyCollection<int>>(2, 1, [1]));
                }

                if (Interlocked.Increment(ref page2Calls) < 2)
                {
                    throw new HttpRequestException("transient last page");
                }

                return Task.FromResult(new PageableResponse<IReadOnlyCollection<int>>(2, 2, [2]));
            };

        var pages = new List<int>();
        await requestFunc
            .BatchProcessAsync<
                TestPageableRequest,
                PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
                request,
                processFunc: r =>
                {
                    pages.Add(r.PageNumber);
                    return Task.CompletedTask;
                },
                pageRetryPolicy: FastRetryPolicy(maxAttempts: 3),
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(new[] { 1, 2 }, pages);
        Assert.Equal(2, page2Calls);
    }

    /// <summary>
    /// На момент вызова <c>processFunc</c> номер страницы в переданном запросе совпадает с номером страницы в полученном ответе.
    /// </summary>
    [Fact]
    public async Task BatchProcessAsync_ResponsePageNumberEqualsRequestPageNumberDuringCall()
    {
        const int batchSize = 10;
        var request = new TestPageableRequest { PageSize = batchSize };
        var pairs = new List<(int Req, int Resp)>();

        await BuildRequestFuncWithToken(totalPages: 2)
            .BatchProcessAsync<
                TestPageableRequest,
                PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
                request,
                processFunc: r =>
                {
                    pairs.Add((request.PageNumber, r.PageNumber));
                    return Task.CompletedTask;
                },
                pageRetryPolicy: null,
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal([(1, 1), (2, 2)], pairs);
    }

    /// <summary>
    /// Отмена токена из пользовательского колбэка после первой страницы приводит к <see cref="OperationCanceledException"/> и прекращает обход.
    /// </summary>
    [Fact]
    public async Task BatchProcessAsync_WhenCanceledAfterFirstPage_StopsWithOperationCanceled()
    {
        const int batchSize = 10;
        using var cts = new CancellationTokenSource();
        var request = new TestPageableRequest { PageSize = batchSize };
        var requestFunc = BuildRequestFuncWithToken(totalPages: 5);
        var processed = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            requestFunc
                .BatchProcessAsync<TestPageableRequest, PageableResponse<IReadOnlyCollection<int>>,
                    IReadOnlyCollection<int>>(
                    request,
                    processFunc: _ =>
                    {
                        if (Interlocked.Increment(ref processed) == 1)
                        {
                            cts.Cancel();
                        }

                        return Task.CompletedTask;
                    },
                    pageRetryPolicy: null,
                    cancellationToken: cts.Token));

        Assert.Equal(1, processed);
    }

    /// <summary>
    /// Число вызовов HTTP-делегата при полном обходе совпадает с <c>TotalPages</c> (нет лишнего запроса после последней страницы в этом сценарии).
    /// </summary>
    [Fact]
    public async Task BatchSelectPagesAsync_WhenHealthy_HttpCallCountEqualsTotalPages()
    {
        const int batchSize = 10;
        var request = new TestPageableRequest { PageSize = batchSize };
        var httpCalls = 0;
        var requestFunc =
            BuildCountingRequestFuncWithToken(totalPages: 3, onCall: () => Interlocked.Increment(ref httpCalls));

        var pages = await requestFunc
            .BatchSelectPagesAsync<TestPageableRequest, PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
                request,
                pageRetryPolicy: null,
                cancellationToken: TestContext.Current.CancellationToken)
            .ToPageNumberListAsync();

        Assert.Equal(new[] { 1, 2, 3 }, pages);
        Assert.Equal(3, httpCalls);
    }

    /// <summary>
    /// Контракт обхода опирается на <c>TotalPages</c>: пустой payload промежуточной страницы сам по себе не завершает цикл, если метаданные сообщают о следующих страницах.
    /// </summary>
    [Fact]
    public async Task BatchSelectPagesAsync_WhenTotalPagesLarge_ContinuesUntilTotalPagesDespiteEmptyPayloadOnLaterPages()
    {
        const int batchSize = 10;
        var request = new TestPageableRequest { PageSize = batchSize };
        var httpCalls = 0;

        Func<TestPageableRequest, CancellationToken, Task<PageableResponse<IReadOnlyCollection<int>>>> requestFunc =
            (req, _) =>
            {
                Interlocked.Increment(ref httpCalls);
                if (req.PageNumber == 1)
                {
                    return Task.FromResult(new PageableResponse<IReadOnlyCollection<int>>(50, 1, [1]));
                }

                return Task.FromResult(new PageableResponse<IReadOnlyCollection<int>>(50, req.PageNumber, []));
            };

        var pages = await requestFunc
            .BatchSelectPagesAsync<TestPageableRequest, PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
                request,
                pageRetryPolicy: null,
                cancellationToken: TestContext.Current.CancellationToken)
            .ToPageNumberListAsync();

        Assert.Equal(Enumerable.Range(1, 50).ToArray(), pages);
        Assert.Equal(50, httpCalls);
    }

    /// <summary>
    /// При выключенной остановке по пустому payload и корректном <c>TotalPages</c> пустая промежуточная страница не обрывает обход преждевременно.
    /// </summary>
    [Fact]
    public async Task BatchSelectPagesAsync_WhenStopWhenPayloadEmptyFalse_ContinuesByTotalPagesDespiteEmptyPayload()
    {
        const int batchSize = 10;
        var request = new TestPageableRequest { PageSize = batchSize };
        var httpCalls = 0;

        Func<TestPageableRequest, CancellationToken, Task<PageableResponse<IReadOnlyCollection<int>>>> requestFunc =
            (req, _) =>
            {
                Interlocked.Increment(ref httpCalls);
                if (req.PageNumber <= 2)
                {
                    var payload = req.PageNumber == 1 ? new[] { 1 } : Array.Empty<int>();
                    return Task.FromResult(new PageableResponse<IReadOnlyCollection<int>>(2, req.PageNumber, payload));
                }

                return Task.FromResult(
                    new PageableResponse<IReadOnlyCollection<int>>(2, req.PageNumber, [req.PageNumber]));
            };

        var pages = await requestFunc
            .BatchSelectPagesAsync<TestPageableRequest, PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
                request,
                pageRetryPolicy: null,
                cancellationToken: TestContext.Current.CancellationToken)
            .ToPageNumberListAsync();

        Assert.Equal(new[] { 1, 2 }, pages);
        Assert.Equal(2, httpCalls);
    }

    /// <summary>
    /// Валидация номера страницы.
    /// </summary>
    [Theory]
    [MemberData(nameof(BatchSelectPagesAsyncRequestPageNumberValidationCases))]
    public async Task BatchSelectPagesAsync_RequestPageNumberValidation(int pageNumber)
    {
        const int batchSize = 10;
        var request = new TestPageableRequest
        {
            PageNumber = pageNumber,
            PageSize = batchSize,
        };
        var requestFunc = BuildRequestFunc(10);
        Exception? exception = null;
        try
        {
            await requestFunc
                .BatchSelectPagesAsync<TestPageableRequest, PageableResponse<IReadOnlyCollection<int>>,
                    IReadOnlyCollection<int>>(
                    request,
                    pageRetryPolicy: null,
                    cancellationToken: TestContext.Current.CancellationToken)
                .ToPageNumberListAsync();
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        if (pageNumber < 1)
        {
            Assert.IsType<ArgumentOutOfRangeException>(exception);
        }
        else
        {
            Assert.Null(exception);
        }
    }
}
