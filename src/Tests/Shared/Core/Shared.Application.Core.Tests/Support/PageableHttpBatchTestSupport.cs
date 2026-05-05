using Shared.Application.Core.Batch.Http.Extensions;
using Shared.Application.Core.Batch.Http.RetryPolicy;
using Shared.Application.Core.Batch.Http.RetryPolicy.Models;
using Shared.Application.Core.Dto.Requests;
using Shared.Application.Core.Dto.Responses;
using Shared.Common.Extensions;

namespace Shared.Application.Core.Tests.Support;

/// <summary>
/// Минимальный <see cref="PageableRequest"/> для юнит-тестов.
/// </summary>
public sealed record TestPageableRequest
    : PageableRequest;

/// <summary>
/// Общие заглушки и хелперы для тестов постраничного HTTP/батчинга.
/// </summary>
public static class PageableHttpBatchTestSupport
{
    /// <summary>
    /// Короткие задержки для тестов повторов (без реальных секунд).
    /// </summary>
    public static RetryConfiguration FastRetry(int maxAttempts = 3) =>
        new()
        {
            Backoff = new BackoffConfiguration
            {
                MaxAttempts = maxAttempts,
                InitialDelay = TimeSpan.FromMilliseconds(1),
                MaxDelay = TimeSpan.FromMilliseconds(5),
            },
        };

    /// <summary>
    /// Короткие задержки как типовая политика повторов для тестов.
    /// </summary>
    public static DefaultHttpBatchRetryPolicy FastRetryPolicy(int maxAttempts = 3) =>
        new(FastRetry(maxAttempts));

    public static Func<TestPageableRequest, CancellationToken, Task<PageableResponse<IReadOnlyCollection<int>>>> BuildRequestFuncWithToken(
        int totalPages)
    {
        return (request, _) =>
        {
            var payload = new[] { request.PageNumber };
            var response = new PageableResponse<IReadOnlyCollection<int>>(totalPages, request.PageNumber, payload);
            return Task.FromResult(response);
        };
    }

    public static Func<TestPageableRequest, CancellationToken, Task<PageableResponse<IReadOnlyCollection<int>>>> BuildCountingRequestFuncWithToken(
        int totalPages,
        Action onCall)
    {
        return (request, _) =>
        {
            onCall();
            var payload = new[] { request.PageNumber };
            var response = new PageableResponse<IReadOnlyCollection<int>>(totalPages, request.PageNumber, payload);
            return Task.FromResult(response);
        };
    }

    public static Func<TestPageableRequest, Task<PageableResponse<IReadOnlyCollection<int>>>> BuildRequestFunc(int totalPages)
    {
        return request =>
        {
            var payload = new[] { request.PageNumber };
            var response = new PageableResponse<IReadOnlyCollection<int>>(totalPages, request.PageNumber, payload);
            return Task.FromResult(response);
        };
    }

    public static Task ExecuteBatchProcessAsync(
        Func<TestPageableRequest, CancellationToken, Task<PageableResponse<IReadOnlyCollection<int>>>> requestFunc,
        TestPageableRequest request,
        Func<PageableResponse<IReadOnlyCollection<int>>, Task> processFunc)
    {
        return requestFunc
            .BatchProcessAsync<
                TestPageableRequest,
                PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
            request,
            processFunc,
            pageRetryPolicy: null,
            cancellationToken: TestContext.Current.CancellationToken);
    }

    public static Task ExecuteBatchProcessAsync(
        Func<TestPageableRequest, Task<PageableResponse<IReadOnlyCollection<int>>>> requestFunc,
        TestPageableRequest request,
        Func<PageableResponse<IReadOnlyCollection<int>>, Task> processFunc)
    {
        return requestFunc
            .BatchProcessAsync<
                TestPageableRequest,
                PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
            request,
            processFunc,
            pageRetryPolicy: null,
            cancellationToken: TestContext.Current.CancellationToken);
    }

    public static IAsyncEnumerable<PageableResponse<IReadOnlyCollection<int>>> ExecuteBatchSelectPagesAsync(
        Func<TestPageableRequest, Task<PageableResponse<IReadOnlyCollection<int>>>> requestFunc,
        TestPageableRequest request)
    {
        return requestFunc
            .BatchSelectPagesAsync<
                TestPageableRequest,
                PageableResponse<IReadOnlyCollection<int>>,
                IReadOnlyCollection<int>>(
            request,
            pageRetryPolicy: null,
            cancellationToken: TestContext.Current.CancellationToken);
    }

    public static async Task<List<int>> ToPageNumberListAsync(
        this IAsyncEnumerable<PageableResponse<IReadOnlyCollection<int>>> source)
    {
        var list = new List<int>();
        await source.ForEachAsync(
            page =>
            {
                list.Add(page.PageNumber);
                return Task.CompletedTask;
            },
            TestContext.Current.CancellationToken);
        return list;
    }
}
