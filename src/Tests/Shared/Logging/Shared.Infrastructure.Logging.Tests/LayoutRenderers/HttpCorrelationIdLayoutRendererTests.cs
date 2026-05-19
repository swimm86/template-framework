using Microsoft.AspNetCore.Http;
using NLog;
using Shared.Infrastructure.Logging.LayoutRenderers;

namespace Shared.Infrastructure.Logging.Tests.LayoutRenderers;

public sealed class HttpCorrelationIdLayoutRendererTests
{
    private static HttpCorrelationIdLayoutRenderer CreateRenderer(IHttpContextAccessor? accessor = null)
    {
        return new HttpCorrelationIdLayoutRenderer { HttpContextAccessor = accessor };
    }

    [Fact]
    public void Append_WhenCorrelationIdExists_WritesToBuilder()
    {
        var correlationId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[Shared.Application.Core.CorrelationId.Constants.CorrelationIdHeader] = correlationId.ToString();
        var renderer = CreateRenderer(new HttpContextAccessor { HttpContext = httpContext });

        var result = renderer.Render(LogEventInfo.CreateNullEvent());

        result.Should().Be(correlationId.ToString());
    }

    [Fact]
    public void Append_WhenCorrelationIdMissing_WritesNothing()
    {
        var httpContext = new DefaultHttpContext();
        var renderer = CreateRenderer(new HttpContextAccessor { HttpContext = httpContext });

        var result = renderer.Render(LogEventInfo.CreateNullEvent());

        result.Should().BeEmpty();
    }

    [Fact]
    public void Append_WhenCorrelationIdIsEmpty_WritesNothing()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[Shared.Application.Core.CorrelationId.Constants.CorrelationIdHeader] = string.Empty;
        var renderer = CreateRenderer(new HttpContextAccessor { HttpContext = httpContext });

        var result = renderer.Render(LogEventInfo.CreateNullEvent());

        result.Should().BeEmpty();
    }

    [Fact]
    public void Append_WhenHttpContextIsNull_DoesNotThrow()
    {
        var renderer = CreateRenderer();

        var act = () => renderer.Render(LogEventInfo.CreateNullEvent());

        act.Should().NotThrow();
        renderer.Render(LogEventInfo.CreateNullEvent()).Should().BeEmpty();
    }

    [Fact]
    public void Append_WhenHttpContextAccessorIsNull_DoesNotThrow()
    {
        var renderer = CreateRenderer(null);

        var act = () => renderer.Render(LogEventInfo.CreateNullEvent());

        act.Should().NotThrow();
        renderer.Render(LogEventInfo.CreateNullEvent()).Should().BeEmpty();
    }
}
