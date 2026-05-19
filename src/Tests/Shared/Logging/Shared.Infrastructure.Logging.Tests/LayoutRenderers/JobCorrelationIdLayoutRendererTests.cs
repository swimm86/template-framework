using NLog;
using Shared.Application.Core.CorrelationId;
using Shared.Infrastructure.Logging.LayoutRenderers;

namespace Shared.Infrastructure.Logging.Tests.LayoutRenderers;

public sealed class JobCorrelationIdLayoutRendererTests
{
    private readonly JobCorrelationIdLayoutRenderer _renderer = new();

    public JobCorrelationIdLayoutRendererTests()
    {
        JobCorrelationContext.ClearCorrelationId();
    }

    [Fact]
    public void Append_WhenCorrelationSet_WritesToBuilder()
    {
        JobCorrelationContext.TrySetCorrelationId();
        var correlationId = JobCorrelationContext.GetCorrelationId();

        var result = _renderer.Render(LogEventInfo.CreateNullEvent());

        result.Should().Be(correlationId!.Value.ToString());
    }

    [Fact]
    public void Append_WhenCorrelationNotSet_WritesNothing()
    {
        var result = _renderer.Render(LogEventInfo.CreateNullEvent());

        result.Should().BeEmpty();
    }

    [Fact]
    public void SetAndClear_CorrelationId_ReadsThenEmpty()
    {
        JobCorrelationContext.TrySetCorrelationId();
        var resultWhenSet = _renderer.Render(LogEventInfo.CreateNullEvent());
        resultWhenSet.Should().NotBeEmpty();

        JobCorrelationContext.ClearCorrelationId();
        var resultAfterClear = _renderer.Render(LogEventInfo.CreateNullEvent());
        resultAfterClear.Should().BeEmpty();
    }

    [Fact]
    public void Append_WhenCorrelationSetTwice_OnlyFirstValueUsed()
    {
        _ = JobCorrelationContext.TrySetCorrelationId();
        var firstId = JobCorrelationContext.GetCorrelationId();
        var second = JobCorrelationContext.TrySetCorrelationId();
        second.Should().BeFalse();

        var result = _renderer.Render(LogEventInfo.CreateNullEvent());

        result.Should().Be(firstId!.Value.ToString());
    }
}
