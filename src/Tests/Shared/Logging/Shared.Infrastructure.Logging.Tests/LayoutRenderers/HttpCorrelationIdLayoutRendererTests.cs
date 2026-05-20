using Microsoft.AspNetCore.Http;
using NLog;
using Shared.Infrastructure.Logging.LayoutRenderers;

namespace Shared.Infrastructure.Logging.Tests.LayoutRenderers;

/// <summary>
/// Тесты для <see cref="HttpCorrelationIdLayoutRenderer"/>,
/// извлекающего correlationId из HTTP-заголовков запроса.
/// </summary>
public sealed class HttpCorrelationIdLayoutRendererTests
{
    private static HttpCorrelationIdLayoutRenderer CreateRenderer(IHttpContextAccessor? accessor = null)
    {
        return new HttpCorrelationIdLayoutRenderer { HttpContextAccessor = accessor };
    }

    /// <summary>
    /// Проверяет, что при наличии correlationId в заголовке запроса он записывается в вывод рендерера.
    /// </summary>
    [Fact]
    public void Append_WhenCorrelationIdExists_WritesToBuilder()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[Application.Core.CorrelationId.Constants.CorrelationIdHeader] = correlationId.ToString();
        var renderer = CreateRenderer(new HttpContextAccessor { HttpContext = httpContext });

        // Act
        var result = renderer.Render(LogEventInfo.CreateNullEvent());

        // Assert
        result.Should().Be(correlationId.ToString());
    }

    /// <summary>
    /// Проверяет, что при отсутствии заголовка correlationId рендерер возвращает пустую строку.
    /// </summary>
    [Fact]
    public void Append_WhenCorrelationIdMissing_WritesNothing()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var renderer = CreateRenderer(new HttpContextAccessor { HttpContext = httpContext });

        // Act
        var result = renderer.Render(LogEventInfo.CreateNullEvent());

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет, что при пустом значении заголовка correlationId рендерер возвращает пустую строку.
    /// </summary>
    [Fact]
    public void Append_WhenCorrelationIdIsEmpty_WritesNothing()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[Shared.Application.Core.CorrelationId.Constants.CorrelationIdHeader] = string.Empty;
        var renderer = CreateRenderer(new HttpContextAccessor { HttpContext = httpContext });

        // Act
        var result = renderer.Render(LogEventInfo.CreateNullEvent());

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет, что при null-значении HttpContext рендерер не выбрасывает исключение.
    /// </summary>
    [Fact]
    public void Append_WhenHttpContextIsNull_DoesNotThrow()
    {
        // Arrange
        var renderer = CreateRenderer();

        // Act & Assert
        var act = () => renderer.Render(LogEventInfo.CreateNullEvent());

        act.Should().NotThrow();
        renderer.Render(LogEventInfo.CreateNullEvent()).Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет, что при null-значении IHttpContextAccessor рендерер не выбрасывает исключение.
    /// </summary>
    [Fact]
    public void Append_WhenHttpContextAccessorIsNull_DoesNotThrow()
    {
        // Arrange
        var renderer = CreateRenderer(accessor:null);

        // Act & Assert
        var act = () => renderer.Render(LogEventInfo.CreateNullEvent());

        act.Should().NotThrow();
        renderer.Render(LogEventInfo.CreateNullEvent()).Should().BeEmpty();
    }
}
