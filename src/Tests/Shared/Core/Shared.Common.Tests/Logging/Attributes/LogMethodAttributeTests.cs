// ----------------------------------------------------------------------------------------------
// <copyright file="LogMethodAttributeTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Shared.Common.Logging.Attributes;
using Shared.Testing.Doubles.Logging;

namespace Shared.Common.Tests.Logging.Attributes;

/// <summary>
/// Тесты для <see cref="LogMethodAttribute"/>.
/// </summary>
public sealed class LogMethodAttributeTests
{
    [Fact]
    public void Constructor_DefaultValues_DoesNotThrow()
    {
        var attribute = new TestableLogMethodAttribute();

        attribute.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomProcessDescription_DoesNotThrow()
    {
        var attribute = new TestableLogMethodAttribute("CustomProcess", true, LogLevel.Warning);

        attribute.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithLogLevel_DoesNotThrow()
    {
        var attribute = new TestableLogMethodAttribute(logLevel: LogLevel.Debug);

        attribute.Should().NotBeNull();
    }

    [Fact]
    public void OnLogStarted_DefaultParameters_LogsAtInformationLevel()
    {
        var attribute = new TestableLogMethodAttribute();
        var fakeLogger = new FakeLogger();

        attribute.InvokeOnLogStarted(fakeLogger, "TestMethod");

        fakeLogger.Entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Information &&
            e.Message.Contains("started"));
    }

    [Fact]
    public void OnLogStarted_WithCustomLogLevel_LogsAtSpecifiedLevel()
    {
        var attribute = new TestableLogMethodAttribute(logLevel: LogLevel.Warning);
        var fakeLogger = new FakeLogger();

        attribute.InvokeOnLogStarted(fakeLogger, "TestMethod");

        fakeLogger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public void OnLogCompleted_DefaultParameters_LogsAtInformationLevel()
    {
        var attribute = new TestableLogMethodAttribute();
        var fakeLogger = new FakeLogger();

        attribute.InvokeOnLogCompleted(fakeLogger, "TestMethod");

        fakeLogger.Entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Information &&
            e.Message.Contains("completed"));
    }

    [Fact]
    public void OnLogFailed_LogsErrorWithException()
    {
        var attribute = new TestableLogMethodAttribute();
        var fakeLogger = new FakeLogger();
        var exception = new InvalidOperationException("test");

        attribute.InvokeOnLogFailed(fakeLogger, "TestMethod", exception);

        fakeLogger.Entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Error &&
            e.Exception == exception &&
            e.Message.Contains("failed"));
    }

    [Fact]
    public void OnLogStarted_WithNullLogger_DoesNotThrow()
    {
        var attribute = new TestableLogMethodAttribute();

        var act = () => attribute.InvokeOnLogStarted(null, "TestMethod");

        act.Should().NotThrow();
    }

    private sealed class TestableLogMethodAttribute : LogMethodAttribute
    {
        public TestableLogMethodAttribute(
            string processDescription = "",
            bool logProcessedTime = true,
            LogLevel logLevel = LogLevel.Information)
            : base(processDescription, logProcessedTime, logLevel)
        {
        }

        public void InvokeOnLogStarted(ILogger? logger, string process) => OnLogStarted(logger, process);

        public void InvokeOnLogCompleted(ILogger? logger, string process) => OnLogCompleted(logger, process);

        public void InvokeOnLogFailed(ILogger? logger, string process, Exception exception) =>
            OnLogFailed(logger, process, exception);
    }
}
