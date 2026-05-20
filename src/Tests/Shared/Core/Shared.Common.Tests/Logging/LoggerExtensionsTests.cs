// ----------------------------------------------------------------------------------------------
// <copyright file="LoggerExtensionsTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Shared.Common.Logging.Extensions;
using Shared.Testing.Doubles.Logging;

namespace Shared.Common.Tests.Logging;

/// <summary>
/// Тесты для расширений <see cref="LoggerExtensions"/>.
/// </summary>
public sealed class LoggerExtensionsTests
{
    private readonly FakeLogger _logger = new();

    /// <summary>
    /// LogTaskAsync с результатом логирует start и completed, возвращает значение.
    /// </summary>
    [Fact]
    public void LogTaskAsync_WithResult_Success_LogsStartAndCompleted()
    {
        // Act
        var result = _logger.LogTaskAsync(() => Task.FromResult(42)).GetAwaiter().GetResult();

        // Assert
        result.Should().Be(42);
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("started"));
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("completed"));
    }

    /// <summary>
    /// LogTaskAsync с результатом возвращает правильное значение.
    /// </summary>
    [Fact]
    public async Task LogTaskAsync_WithResult_ReturnsCorrectValue()
    {
        // Act
        var result = await _logger.LogTaskAsync(() => Task.FromResult("hello"));

        // Assert
        result.Should().Be("hello");
    }

    /// <summary>
    /// LogTaskAsync при ошибке логирует failed и пробрасывает исключение.
    /// </summary>
    [Fact]
    public async Task LogTaskAsync_WhenActionThrows_LogsFailedAndRethrows()
    {
        // Act
        var act = () => _logger.LogTaskAsync<object>(() => throw new InvalidOperationException("test error"));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("failed"));
    }

    /// <summary>
    /// LogTaskAsync с CancellationToken логирует start и completed.
    /// </summary>
    [Fact]
    public async Task LogTaskAsync_WithCancellationToken_Success_LogsStartAndCompleted()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        // Act
        await _logger.LogTaskAsync(() => Task.Delay(10, cts.Token), cts.Token);

        // Assert
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("started"));
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("completed"));
    }

    /// <summary>
    /// LogTask (синхронный) логирует start и completed, возвращает результат.
    /// </summary>
    [Fact]
    public void LogTask_Sync_Success_LogsStartAndCompleted()
    {
        // Act
        var result = _logger.LogTask(() => 99);

        // Assert
        result.Should().Be(99);
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("started"));
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("completed"));
    }

    /// <summary>
    /// LogTask (синхронный) при ошибке логирует failed и пробрасывает исключение.
    /// </summary>
    [Fact]
    public void LogTask_Sync_WhenActionThrows_LogsFailedAndRethrows()
    {
        // Act
        var act = () => _logger.LogTask<object>(() => throw new InvalidOperationException("sync error"));

        // Assert
        act.Should().Throw<InvalidOperationException>();
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("failed"));
    }

    /// <summary>
    /// LogTask (void) логирует start и completed.
    /// </summary>
    [Fact]
    public void LogTask_VoidAction_Success_LogsStartAndCompleted()
    {
        // Arrange
        var executed = false;

        // Act
        _logger.LogTask(() => { executed = true; });

        // Assert
        executed.Should().BeTrue();
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("started"));
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("completed"));
    }

    /// <summary>
    /// LogTaskAsync с logProcessedTime логирует время выполнения.
    /// </summary>
    [Fact]
    public async Task LogTaskAsync_WithLogProcessedTime_LogsElapsed()
    {
        // Act
        await _logger.LogTaskAsync(() => Task.FromResult(1), logProcessedTime: true);

        // Assert
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("processed time"));
    }

    /// <summary>
    /// LogTaskAsync без logProcessedTime не логирует время.
    /// </summary>
    [Fact]
    public async Task LogTaskAsync_WithoutLogProcessedTime_DoesNotLogElapsed()
    {
        // Act
        await _logger.LogTaskAsync(() => Task.FromResult(1), logProcessedTime: false);

        // Assert
        _logger.Entries.Should().NotContain(e => e.Message.Contains("processed time"));
    }

    /// <summary>
    /// LogTaskAsync с описанием процесса использует его в логах.
    /// </summary>
    [Fact]
    public async Task LogTaskAsync_WithProcessDescription_UsesDescriptionInLogs()
    {
        // Arrange
        const string description = "CustomOperation";

        // Act
        await _logger.LogTaskAsync(() => Task.FromResult(1), processDescription: description);

        // Assert
        _logger.Entries.Should().Contain(e => e.Message.Contains(description));
    }

    /// <summary>
    /// LogTaskAsync с null-логгером не выбрасывает исключение.
    /// </summary>
    [Fact]
    public async Task LogTaskAsync_WithNullLogger_DoesNotThrow()
    {
        // Arrange
        ILogger? nullLogger = null;

        // Act
        var act = () => nullLogger.LogTaskAsync(() => Task.FromResult(1));

        // Assert
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// LogTaskAsync с кастомным уровнем логирует на указанном уровне.
    /// </summary>
    [Fact]
    public async Task LogTaskAsync_WithCustomLogLevel_LogsAtSpecifiedLevel()
    {
        // Act
        await _logger.LogTaskAsync(() => Task.FromResult(1), logLevel: LogLevel.Debug);

        // Assert
        _logger.Entries.Should().OnlyContain(e => e.Level == LogLevel.Debug || e.Level == LogLevel.Error);
        _logger.Entries.Where(e => e.Level == LogLevel.Debug).Should().NotBeEmpty();
    }

    /// <summary>
    /// LogTaskAsync при ошибке логирует время на Error.
    /// </summary>
    [Fact]
    public async Task LogTaskAsync_WhenFailed_LogsElapsedAtErrorLevel()
    {
        // Act
        var act = () => _logger.LogTaskAsync<object>(
            () => throw new InvalidOperationException("fail"),
            logProcessedTime: true);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _logger.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Message.Contains("processed time"));
    }
}
