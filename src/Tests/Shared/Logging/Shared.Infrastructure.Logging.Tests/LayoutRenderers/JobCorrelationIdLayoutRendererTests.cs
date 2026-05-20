using NLog;
using Shared.Application.Core.CorrelationId;
using Shared.Infrastructure.Logging.LayoutRenderers;

namespace Shared.Infrastructure.Logging.Tests.LayoutRenderers;

/// <summary>
/// Тесты для <see cref="JobCorrelationIdLayoutRenderer"/>,
/// извлекающего correlationId из <see cref="JobCorrelationContext"/> для фоновых задач.
/// </summary>
public sealed class JobCorrelationIdLayoutRendererTests
{
    private readonly JobCorrelationIdLayoutRenderer _renderer = new();

    public JobCorrelationIdLayoutRendererTests()
    {
        JobCorrelationContext.ClearCorrelationId();
    }

    /// <summary>
    /// Проверяет, что при установленном correlationId в <see cref="JobCorrelationContext"/>
    /// рендерер записывает его значение.
    /// </summary>
    [Fact]
    public void Append_WhenCorrelationSet_WritesToBuilder()
    {
        // Arrange
        JobCorrelationContext.TrySetCorrelationId();
        var correlationId = JobCorrelationContext.GetCorrelationId();

        // Act
        var result = _renderer.Render(LogEventInfo.CreateNullEvent());

        // Assert
        result.Should().Be(correlationId!.Value.ToString());
    }

    /// <summary>
    /// Проверяет, что при отсутствии correlationId в <see cref="JobCorrelationContext"/>
    /// рендерер возвращает пустую строку.
    /// </summary>
    [Fact]
    public void Append_WhenCorrelationNotSet_WritesNothing()
    {
        // Act
        var result = _renderer.Render(LogEventInfo.CreateNullEvent());

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет, что после очистки correlationId рендерер возвращает пустую строку.
    /// </summary>
    [Fact]
    public void SetAndClear_CorrelationId_ReadsThenEmpty()
    {
        // Arrange
        JobCorrelationContext.TrySetCorrelationId();

        // Act
        var resultWhenSet = _renderer.Render(LogEventInfo.CreateNullEvent());

        // Assert
        resultWhenSet.Should().NotBeEmpty();

        // Act
        JobCorrelationContext.ClearCorrelationId();
        var resultAfterClear = _renderer.Render(LogEventInfo.CreateNullEvent());

        // Assert
        resultAfterClear.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет, что при повторной попытке установить correlationId используется только первое значение.
    /// </summary>
    [Fact]
    public void Append_WhenCorrelationSetTwice_OnlyFirstValueUsed()
    {
        // Arrange
        _ = JobCorrelationContext.TrySetCorrelationId();
        var firstId = JobCorrelationContext.GetCorrelationId();
        var second = JobCorrelationContext.TrySetCorrelationId();
        second.Should().BeFalse();

        // Act
        var result = _renderer.Render(LogEventInfo.CreateNullEvent());

        // Assert
        result.Should().Be(firstId!.Value.ToString());
    }
}
