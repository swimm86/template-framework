using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.CorrelationId;
using Shared.Infrastructure.Core.ApiClient.Handlers;

namespace Shared.Infrastructure.Core.Tests.ApiClient.Handlers;

/// <summary>
/// Тесты <see cref="CorrelationIdHeaderDelegatingHandler"/>.
/// </summary>
public sealed class CorrelationIdHeaderDelegatingHandlerTests
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    /// <summary>
    /// Если заголовок <c>X-Correlation-Id</c> уже присутствует в запросе —
    /// обработчик не перезаписывает его и делегирует запрос дальше.
    /// </summary>
    [Fact]
    public async Task SendAsync_HeaderAlreadyPresent_PassesThrough()
    {
        var existingCorrelationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var request = new HttpRequestMessage();
        request.Headers.Add(CorrelationIdHeader, existingCorrelationId.ToString("D"));

        var (httpContextAccessor, logger) = CreateDependencies();
        var stub = new StubHttpMessageHandler();
        var handler = CreateHandler(httpContextAccessor, logger, stub);
        var invoker = new HttpMessageInvoker(handler, disposeHandler: false);

        await invoker.SendAsync(request, CancellationToken.None);

        stub.CapturedRequest.Should().NotBeNull();
        stub.CapturedRequest!.Headers.GetValues(CorrelationIdHeader).Single().Should().Be(existingCorrelationId.ToString("D"));
    }

    /// <summary>
    /// Если заголовок отсутствует, но <see cref="IHttpContextAccessor"/> содержит валидный correlation id —
    /// заголовок добавляется из HttpContext.
    /// </summary>
    [Fact]
    public async Task SendAsync_HeaderMissing_AddsHeaderFromHttpContext()
    {
        var correlationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[CorrelationIdHeader] = correlationId.ToString("D");

        var httpContextAccessor = new StubHttpContextAccessor { HttpContext = httpContext };
        var logger = new FakeLogger<CorrelationIdHeaderDelegatingHandler>();
        var stub = new StubHttpMessageHandler();
        var handler = CreateHandler(httpContextAccessor, logger, stub);
        var invoker = new HttpMessageInvoker(handler, disposeHandler: false);

        var request = new HttpRequestMessage();
        await invoker.SendAsync(request, CancellationToken.None);

        stub.CapturedRequest!.Headers.GetValues(CorrelationIdHeader).Single().Should().Be(correlationId.ToString("D"));
    }

    /// <summary>
    /// Если HttpContext недоступен, но <see cref="JobCorrelationContext"/> содержит значение —
    /// заголовок добавляется из контекста джобы.
    /// </summary>
    [Fact]
    public async Task SendAsync_HeaderMissingAndNoHttpContext_FallsBackToJobCorrelation()
    {
        JobCorrelationContext.TrySetCorrelationId();
        var jobCorrelationId = JobCorrelationContext.GetCorrelationId()!.Value;
        try
        {
            var httpContextAccessor = new StubHttpContextAccessor { HttpContext = null };
            var logger = new FakeLogger<CorrelationIdHeaderDelegatingHandler>();
            var stub = new StubHttpMessageHandler();
            var handler = CreateHandler(httpContextAccessor, logger, stub);
            var invoker = new HttpMessageInvoker(handler, disposeHandler: false);

            var request = new HttpRequestMessage();
            await invoker.SendAsync(request, CancellationToken.None);

            stub.CapturedRequest!.Headers.GetValues(CorrelationIdHeader).Single().Should().Be(jobCorrelationId.ToString("D"));
        }
        finally
        {
            JobCorrelationContext.ClearCorrelationId();
        }
    }

    /// <summary>
    /// Если ни HttpContext, ни JobCorrelationContext не содержат correlation id —
    /// обработчик логирует ошибку и пропускает запрос дальше без добавления заголовка.
    /// </summary>
    [Fact]
    public async Task SendAsync_NoCorrelationSource_LogsErrorAndPassesThrough()
    {
        var httpContextAccessor = new StubHttpContextAccessor { HttpContext = null };
        var logger = new FakeLogger<CorrelationIdHeaderDelegatingHandler>();
        var stub = new StubHttpMessageHandler();
        var handler = CreateHandler(httpContextAccessor, logger, stub);
        var invoker = new HttpMessageInvoker(handler, disposeHandler: false);

        var request = new HttpRequestMessage { RequestUri = new Uri("https://example.com/test") };
        await invoker.SendAsync(request, CancellationToken.None);

        logger.LogEntries.Should().ContainSingle(entry => entry.Level == LogLevel.Error);
        stub.CapturedRequest!.Headers.Contains(CorrelationIdHeader).Should().BeFalse();
        stub.CapturedRequest.Should().NotBeNull();
    }

    /// <summary>
    /// Обработчик не модифицирует прочие заголовки запроса.
    /// </summary>
    [Fact]
    public async Task SendAsync_PreservesOriginalRequestHeaders()
    {
        var correlationId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[CorrelationIdHeader] = correlationId.ToString("D");

        var request = new HttpRequestMessage();
        request.Headers.Add("X-Custom-Header", "custom-value");

        var httpContextAccessor = new StubHttpContextAccessor { HttpContext = httpContext };
        var logger = new FakeLogger<CorrelationIdHeaderDelegatingHandler>();
        var stub = new StubHttpMessageHandler();
        var handler = CreateHandler(httpContextAccessor, logger, stub);
        var invoker = new HttpMessageInvoker(handler, disposeHandler: false);

        await invoker.SendAsync(request, CancellationToken.None);

        stub.CapturedRequest!.Headers.GetValues("X-Custom-Header").Single().Should().Be("custom-value");
        stub.CapturedRequest.Headers.GetValues(CorrelationIdHeader).Single().Should().Be(correlationId.ToString("D"));
    }

    /// <summary>
    /// Обработчик возвращает ответ от внутреннего обработчика.
    /// </summary>
    [Fact]
    public async Task SendAsync_ReturnsInnerHandlerResponse()
    {
        var correlationId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[CorrelationIdHeader] = correlationId.ToString("D");

        var httpContextAccessor = new StubHttpContextAccessor { HttpContext = httpContext };
        var logger = new FakeLogger<CorrelationIdHeaderDelegatingHandler>();
        var stub = new StubHttpMessageHandler();
        var handler = CreateHandler(httpContextAccessor, logger, stub);
        var invoker = new HttpMessageInvoker(handler, disposeHandler: false);

        var request = new HttpRequestMessage();
        var response = await invoker.SendAsync(request, CancellationToken.None);

        response.Should().NotBeNull();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    private static CorrelationIdHeaderDelegatingHandler CreateHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<CorrelationIdHeaderDelegatingHandler> logger,
        HttpMessageHandler innerHandler)
    {
        var handler = new CorrelationIdHeaderDelegatingHandler(httpContextAccessor, logger);
        typeof(DelegatingHandler)
            .GetProperty("InnerHandler", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .SetValue(handler, innerHandler);
        return handler;
    }

    private static (IHttpContextAccessor, ILogger<CorrelationIdHeaderDelegatingHandler>) CreateDependencies()
    {
        var httpContextAccessor = new StubHttpContextAccessor();
        var logger = new FakeLogger<CorrelationIdHeaderDelegatingHandler>();
        return (httpContextAccessor, logger);
    }

    private sealed class StubHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            CapturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    private sealed class FakeLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> LogEntries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LogEntries.Add((logLevel, formatter(state, exception)));
        }
    }
}
