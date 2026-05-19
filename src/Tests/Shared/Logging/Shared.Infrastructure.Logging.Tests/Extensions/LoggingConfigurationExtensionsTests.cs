using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using Shared.Infrastructure.Logging.Constants;
using Shared.Infrastructure.Logging.Extensions;

namespace Shared.Infrastructure.Logging.Tests.Extensions;

public sealed class LoggingConfigurationExtensionsTests
{
    [Fact]
    public void AddCorrelationIdToTargetLayouts_AddsHttpPlaceholder()
    {
        var config = new LoggingConfiguration();
        var target = new FileTarget("testTarget")
        {
            Layout = "${longdate} | ${level} | ${logger} | msg=${message}"
        };
        config.AddTarget(target);

        config.AddCorrelationIdToTargetLayouts();

        var layout = (SimpleLayout)((TargetWithLayout)config.AllTargets[0]).Layout;
        layout.Text.Should().Contain(CorrelationIdScopePropertyKeys.Http);
    }

    [Fact]
    public void AddCorrelationIdToTargetLayouts_AddsJobPlaceholder()
    {
        var config = new LoggingConfiguration();
        var target = new FileTarget("testTarget")
        {
            Layout = "${longdate} | ${level} | ${logger} | msg=${message}"
        };
        config.AddTarget(target);

        config.AddCorrelationIdToTargetLayouts();

        var layout = (SimpleLayout)((TargetWithLayout)config.AllTargets[0]).Layout;
        layout.Text.Should().Contain(CorrelationIdScopePropertyKeys.Job);
    }

    [Fact]
    public void AddCorrelationIdToTargetLayouts_ExcludedTargets_NotModified()
    {
        var config = new LoggingConfiguration();
        var originalLayout = "${longdate} | ${level} | msg=${message}";
        var excludedTarget = new FileTarget("coloredSystemEventConsole")
        {
            Layout = originalLayout
        };
        config.AddTarget(excludedTarget);

        config.AddCorrelationIdToTargetLayouts();

        var layout = (SimpleLayout)((TargetWithLayout)config.AllTargets[0]).Layout;
        layout.Text.Should().Be(originalLayout);
    }

    [Fact]
    public void AddCorrelationIdToTargetLayouts_SecondExcludedTarget_NotModified()
    {
        var config = new LoggingConfiguration();
        var originalLayout = "${longdate} | msg=${message}";
        var excludedTarget = new FileTarget("coloredBusinessEventConsole")
        {
            Layout = originalLayout
        };
        config.AddTarget(excludedTarget);

        config.AddCorrelationIdToTargetLayouts();

        var layout = (SimpleLayout)((TargetWithLayout)config.AllTargets[0]).Layout;
        layout.Text.Should().Be(originalLayout);
    }

    [Fact]
    public void AddCorrelationIdToTargetLayouts_AlreadyContainsCorrelationId_NotDuplicated()
    {
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

        config.AddCorrelationIdToTargetLayouts();

        var layout = (SimpleLayout)((TargetWithLayout)config.AllTargets[0]).Layout;
        var count = CountSubstring(layout.Text, CorrelationIdScopePropertyKeys.Http);
        count.Should().Be(2, "placeholder should appear exactly twice (as in original layout)");
    }

    [Fact]
    public void AddCorrelationIdToTargetLayouts_IsIdempotent_NoExceptionOnSecondCall()
    {
        var config = new LoggingConfiguration();
        var target = new FileTarget("testTarget")
        {
            Layout = "${longdate} | ${level} | msg=${message}"
        };
        config.AddTarget(target);

        config.AddCorrelationIdToTargetLayouts();
        var act = () => config.AddCorrelationIdToTargetLayouts();

        act.Should().NotThrow();
    }

    [Fact]
    public void AddCorrelationIdToTargetLayouts_WithoutMessagePattern_AppendsToEnd()
    {
        var config = new LoggingConfiguration();
        var target = new FileTarget("testTarget")
        {
            Layout = "${longdate} | ${level}"
        };
        config.AddTarget(target);

        config.AddCorrelationIdToTargetLayouts();

        var layout = (SimpleLayout)((TargetWithLayout)config.AllTargets[0]).Layout;
        layout.Text.Should().StartWith("${longdate} | ${level}");
        layout.Text.Should().Contain(CorrelationIdScopePropertyKeys.Http);
        layout.Text.Should().Contain(CorrelationIdScopePropertyKeys.Job);
    }

    [Fact]
    public void AddCorrelationIdToTargetLayouts_NonTargetWithLayout_Skipped()
    {
        var config = new LoggingConfiguration();

        config.AddCorrelationIdToTargetLayouts();

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
