using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using Shared.Infrastructure.Logging.Constants;
using Shared.Infrastructure.Logging.Extensions;

namespace Shared.Infrastructure.Logging.Tests.Extensions;

/// <summary>
/// Тесты для расширения <see cref="LoggingConfigurationExtensions"/>,
/// добавляющего placeholders correlationId в layouts NLog-таргетов.
/// </summary>
public sealed class LoggingConfigurationExtensionsTests
{
    /// <summary>
    /// Проверяет, что после вызова метода в layout добавляется placeholder для HTTP correlationId.
    /// </summary>
    [Fact]
    public void AddCorrelationIdToTargetLayouts_AddsHttpPlaceholder()
    {
        // Arrange
        var config = new LoggingConfiguration();
        var target = new FileTarget("testTarget")
        {
            Layout = "${longdate} | ${level} | ${logger} | msg=${message}"
        };
        config.AddTarget(target);

        // Act
        config.AddCorrelationIdToTargetLayouts();

        // Assert
        var layout = (SimpleLayout)((TargetWithLayout)config.AllTargets[0]).Layout;
        layout.Text.Should().Contain(CorrelationIdScopePropertyKeys.Http);
    }

    /// <summary>
    /// Проверяет, что после вызова метода в layout добавляется placeholder для Job correlationId.
    /// </summary>
    [Fact]
    public void AddCorrelationIdToTargetLayouts_AddsJobPlaceholder()
    {
        // Arrange
        var config = new LoggingConfiguration();
        var target = new FileTarget("testTarget")
        {
            Layout = "${longdate} | ${level} | ${logger} | msg=${message}"
        };
        config.AddTarget(target);

        // Act
        config.AddCorrelationIdToTargetLayouts();

        // Assert
        var layout = (SimpleLayout)((TargetWithLayout)config.AllTargets[0]).Layout;
        layout.Text.Should().Contain(CorrelationIdScopePropertyKeys.Job);
    }

    /// <summary>
    /// Проверяет, что таргет с именем "coloredSystemEventConsole" (исключённый) не модифицируется.
    /// </summary>
    [Fact]
    public void AddCorrelationIdToTargetLayouts_ExcludedTargets_NotModified()
    {
        // Arrange
        var config = new LoggingConfiguration();
        var originalLayout = "${longdate} | ${level} | msg=${message}";
        var excludedTarget = new FileTarget("coloredSystemEventConsole")
        {
            Layout = originalLayout
        };
        config.AddTarget(excludedTarget);

        // Act
        config.AddCorrelationIdToTargetLayouts();

        // Assert
        var layout = (SimpleLayout)((TargetWithLayout)config.AllTargets[0]).Layout;
        layout.Text.Should().Be(originalLayout);
    }

    /// <summary>
    /// Проверяет, что таргет с именем "coloredBusinessEventConsole" (исключённый) не модифицируется.
    /// </summary>
    [Fact]
    public void AddCorrelationIdToTargetLayouts_SecondExcludedTarget_NotModified()
    {
        // Arrange
        var config = new LoggingConfiguration();
        var originalLayout = "${longdate} | msg=${message}";
        var excludedTarget = new FileTarget("coloredBusinessEventConsole")
        {
            Layout = originalLayout
        };
        config.AddTarget(excludedTarget);

        // Act
        config.AddCorrelationIdToTargetLayouts();

        // Assert
        var layout = (SimpleLayout)((TargetWithLayout)config.AllTargets[0]).Layout;
        layout.Text.Should().Be(originalLayout);
    }

    /// <summary>
    /// Проверяет, что если layout уже содержит placeholders correlationId,
    /// повторный вызов не дублирует их.
    /// </summary>
    [Fact]
    public void AddCorrelationIdToTargetLayouts_AlreadyContainsCorrelationId_NotDuplicated()
    {
        // Arrange
        var config = new LoggingConfiguration();
        var httpKey = CorrelationIdScopePropertyKeys.Http;
        var jobKey = CorrelationIdScopePropertyKeys.Job;
        var existingBlock =
            "${when:when='${" + httpKey + "}'!='':inner= corId=${" + httpKey + "}}}" +
            "${when:when='${" + jobKey + "}'!='':inner= corId=${" + jobKey + "}}}";
        var layoutWithPlaceholder = "${longdate} | ${level} | msg=${message}" + existingBlock;
        var target = new FileTarget("testTarget")
        {
            Layout = layoutWithPlaceholder
        };
        config.AddTarget(target);

        // Act
        config.AddCorrelationIdToTargetLayouts();

        // Assert
        var layout = (SimpleLayout)((TargetWithLayout)config.AllTargets[0]).Layout;
        var count = CountSubstring(layout.Text, CorrelationIdScopePropertyKeys.Http);
        count.Should().Be(2, "placeholder should appear exactly twice (as in original layout)");
    }

    /// <summary>
    /// Проверяет идемпотентность метода: повторный вызов не выбрасывает исключение.
    /// </summary>
    [Fact]
    public void AddCorrelationIdToTargetLayouts_IsIdempotent_NoExceptionOnSecondCall()
    {
        // Arrange
        var config = new LoggingConfiguration();
        var target = new FileTarget("testTarget")
        {
            Layout = "${longdate} | ${level} | msg=${message}"
        };
        config.AddTarget(target);

        config.AddCorrelationIdToTargetLayouts();

        // Act
        var act = () => config.AddCorrelationIdToTargetLayouts();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Проверяет, что если layout не содержит шаблон "${message}", placeholder добавляется в конец.
    /// </summary>
    [Fact]
    public void AddCorrelationIdToTargetLayouts_WithoutMessagePattern_AppendsToEnd()
    {
        // Arrange
        var config = new LoggingConfiguration();
        var target = new FileTarget("testTarget")
        {
            Layout = "${longdate} | ${level}"
        };
        config.AddTarget(target);

        // Act
        config.AddCorrelationIdToTargetLayouts();

        // Assert
        var layout = (SimpleLayout)((TargetWithLayout)config.AllTargets[0]).Layout;
        layout.Text.Should().StartWith("${longdate} | ${level}");
        layout.Text.Should().Contain(CorrelationIdScopePropertyKeys.Http);
        layout.Text.Should().Contain(CorrelationIdScopePropertyKeys.Job);
    }

    /// <summary>
    /// Проверяет, что при отсутствии таргетов в конфигурации метод не падает.
    /// </summary>
    [Fact]
    public void AddCorrelationIdToTargetLayouts_NonTargetWithLayout_Skipped()
    {
        // Arrange
        var config = new LoggingConfiguration();

        // Act
        config.AddCorrelationIdToTargetLayouts();

        // Assert
        config.AllTargets.Should().BeEmpty();
    }

    private static int CountSubstring(string text, string substring)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(substring, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += substring.Length;
        }

        return count;
    }
}
