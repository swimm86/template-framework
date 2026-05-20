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
    /// <summary>
    /// Конструктор со значениями по умолчанию не выбрасывает исключение.
    /// </summary>
    [Fact]
    public void Constructor_DefaultValues_DoesNotThrow()
    {
        // Act
        var attribute = new TestableLogMethodAttribute();

        // Assert
        attribute.Should().NotBeNull();
    }

    /// <summary>
    /// Конструктор с кастомным описанием процесса не выбрасывает исключение.
    /// </summary>
    [Fact]
    public void Constructor_WithCustomProcessDescription_DoesNotThrow()
    {
        // Act
        var attribute = new TestableLogMethodAttribute("CustomProcess", true, LogLevel.Warning);

        // Assert
        attribute.Should().NotBeNull();
    }

    /// <summary>
    /// Конструктор с указанным уровнем логирования не выбрасывает исключение.
    /// </summary>
    [Fact]
    public void Constructor_WithLogLevel_DoesNotThrow()
    {
        // Act
        var attribute = new TestableLogMethodAttribute(logLevel: LogLevel.Debug);

        // Assert
        attribute.Should().NotBeNull();
    }

    /// <summary>
    /// OnLogStarted по умолчанию логирует на Information с сообщением "started".
    /// </summary>
    [Fact]
    public void OnLogStarted_DefaultParameters_LogsAtInformationLevel()
    {
        // Arrange
        var attribute = new TestableLogMethodAttribute();
        var fakeLogger = new FakeLogger();

        // Act
        attribute.InvokeOnLogStarted(fakeLogger, "TestMethod");

        // Assert
        fakeLogger.Entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Information &&
            e.Message.Contains("started"));
    }

    /// <summary>
    /// OnLogStarted с кастомным уровнем логирует на указанном уровне.
    /// </summary>
    [Fact]
    public void OnLogStarted_WithCustomLogLevel_LogsAtSpecifiedLevel()
    {
        // Arrange
        var attribute = new TestableLogMethodAttribute(logLevel: LogLevel.Warning);
        var fakeLogger = new FakeLogger();

        // Act
        attribute.InvokeOnLogStarted(fakeLogger, "TestMethod");

        // Assert
        fakeLogger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning);
    }

    /// <summary>
    /// OnLogCompleted по умолчанию логирует на Information с сообщением "completed".
    /// </summary>
    [Fact]
    public void OnLogCompleted_DefaultParameters_LogsAtInformationLevel()
    {
        // Arrange
        var attribute = new TestableLogMethodAttribute();
        var fakeLogger = new FakeLogger();

        // Act
        attribute.InvokeOnLogCompleted(fakeLogger, "TestMethod");

        // Assert
        fakeLogger.Entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Information &&
            e.Message.Contains("completed"));
    }

    /// <summary>
    /// OnLogFailed логирует ошибку с исключением.
    /// </summary>
    [Fact]
    public void OnLogFailed_LogsErrorWithException()
    {
        // Arrange
        var attribute = new TestableLogMethodAttribute();
        var fakeLogger = new FakeLogger();
        var exception = new InvalidOperationException("test");

        // Act
        attribute.InvokeOnLogFailed(fakeLogger, "TestMethod", exception);

        // Assert
        fakeLogger.Entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Error &&
            e.Exception == exception &&
            e.Message.Contains("failed"));
    }

    /// <summary>
    /// OnLogStarted с null-логгером не выбрасывает исключение.
    /// </summary>
    [Fact]
    public void OnLogStarted_WithNullLogger_DoesNotThrow()
    {
        // Arrange
        var attribute = new TestableLogMethodAttribute();

        // Act
        var act = () => attribute.InvokeOnLogStarted(null, "TestMethod");

        // Assert
        act.Should().NotThrow();
    }

    private sealed class TestableLogMethodAttribute(
        string processDescription = "",
        bool logProcessedTime = true,
        LogLevel logLevel = LogLevel.Information)
        : LogMethodAttribute(processDescription, logProcessedTime, logLevel)
    {
        public void InvokeOnLogStarted(ILogger? logger, string process) => OnLogStarted(logger, process);

        public void InvokeOnLogCompleted(ILogger? logger, string process) => OnLogCompleted(logger, process);

        public void InvokeOnLogFailed(ILogger? logger, string process, Exception exception) =>
            OnLogFailed(logger, process, exception);
    }
}
