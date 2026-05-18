using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Core.Dto.Responses;
using Shared.Presentation.Core.Exceptions;
using Shared.Presentation.Core.Tests.Infrastructure.Stubs;
using System.Text.Json;

namespace Shared.Presentation.Core.Tests.Exceptions;

/// <summary>
/// Тесты <see cref="Shared.Presentation.Core.Exceptions.ExceptionHandler"/>.
/// </summary>
public sealed class ExceptionHandlerTests
{
    /// <summary>
    /// Проверяет установку статус-кода HTTP-ответа из ErrorResponse.
    /// </summary>
    [Fact]
    public async Task TryHandleAsync_SetsResponseStatusCodeFromMappedResponse()
    {
        // Arrange
        var handler = CreateExceptionHandler(StatusCodes.Status418ImATeapot);
        var httpContext = CreateHttpContextWithResponseStream();

        // Act
        await handler.TryHandleAsync(httpContext, new Exception(), CancellationToken.None);

        // Assert
        httpContext.Response.StatusCode.Should().Be(418);
    }

    /// <summary>
    /// Проверяет, что TryHandleAsync всегда возвращает true.
    /// </summary>
    [Fact]
    public async Task TryHandleAsync_AlwaysReturnsTrue()
    {
        // Arrange
        var handler = CreateExceptionHandler(StatusCodes.Status500InternalServerError);
        var httpContext = CreateHttpContextWithResponseStream();

        // Act
        var result = await handler.TryHandleAsync(httpContext, new Exception(), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что тело ответа содержит сериализованный ErrorResponse.
    /// </summary>
    [Fact]
    public async Task TryHandleAsync_WritesJsonBodyMatchingErrorResponse()
    {
        // Arrange
        var handler = CreateExceptionHandler(
            StatusCodes.Status200OK,
            [new ProblemDetails { Title = "Test", Detail = "D" }],
            "extra");
        var httpContext = CreateHttpContextWithResponseStream();

        // Act
        await handler.TryHandleAsync(httpContext, new Exception(), CancellationToken.None);
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(httpContext.Response.Body)
            .ReadToEndAsync(TestContext.Current.CancellationToken);
        var deserialized = JsonSerializer.Deserialize<ErrorResponse>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Errors.Should().ContainSingle()
            .Which.Title.Should().Be("Test");
        deserialized.Errors.Should().ContainSingle()
            .Which.Detail.Should().Be("D");
        deserialized.Details.Should().Be("extra");
    }

    /// <summary>
    /// Проверяет, что исходное исключение передаётся в резолвер.
    /// </summary>
    [Fact]
    public async Task TryHandleAsync_PassesExceptionToResolver()
    {
        // Arrange
        var (handler, resolver) = CreateExceptionHandlerAndResolver(StatusCodes.Status500InternalServerError);
        var httpContext = CreateHttpContextWithResponseStream();
        var exception = new InvalidOperationException("test-ex");

        // Act
        await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        resolver.ReceivedExceptions.Should().ContainSingle()
            .Which.Should().BeSameAs(exception);
    }

    /// <summary>
    /// Проверяет учёт токена отмены при записи ответа.
    /// </summary>
    [Fact]
    public async Task TryHandleAsync_HonorsCancellationToken()
    {
        // Arrange
        var handler = CreateExceptionHandler(StatusCodes.Status500InternalServerError);
        var httpContext = CreateHttpContextWithResponseStream();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = () => handler.TryHandleAsync(httpContext, new Exception(), cts.Token).AsTask();

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Проверяет, что Content-Type ответа устанавливается в application/json.
    /// </summary>
    [Fact]
    public async Task TryHandleAsync_SetsContentTypeJson()
    {
        // Arrange
        var handler = CreateExceptionHandler(StatusCodes.Status200OK);
        var httpContext = CreateHttpContextWithResponseStream();

        // Act
        await handler.TryHandleAsync(httpContext, new Exception(), CancellationToken.None);

        // Assert
        httpContext.Response.ContentType.Should().StartWith("application/json");
    }

    private ExceptionHandler CreateExceptionHandler(
        int statusCode,
        IReadOnlyCollection<ProblemDetails>? errors = null,
        string? details = null)
    {
        return CreateExceptionHandlerAndResolver(statusCode, errors, details).handler;
    }

    private (ExceptionHandler handler, StubExceptionMapperResolver resolver) CreateExceptionHandlerAndResolver(
        int statusCode,
        IReadOnlyCollection<ProblemDetails>? errors = null,
        string? details = null)
    {
        var response = new ErrorResponse
        {
            StatusCode = statusCode,
            Errors = errors ?? [],
            Details = details,
        };
        var resolver = new StubExceptionMapperResolver(response);
        var handler = new ExceptionHandler(resolver);
        return (handler, resolver);
    }

    private DefaultHttpContext CreateHttpContextWithResponseStream()
    {
        var context = new DefaultHttpContext
        {
            Response =
            {
                Body = new MemoryStream(),
            },
        };
        return context;
    }
}
