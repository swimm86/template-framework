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

    [Fact]
    public void LogTaskAsync_WithResult_Success_LogsStartAndCompleted()
    {
        var result = _logger.LogTaskAsync(() => Task.FromResult(42)).GetAwaiter().GetResult();

        result.Should().Be(42);
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("started"));
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("completed"));
    }

    [Fact]
    public async Task LogTaskAsync_WithResult_ReturnsCorrectValue()
    {
        var result = await _logger.LogTaskAsync(() => Task.FromResult("hello"));

        result.Should().Be("hello");
    }

    [Fact]
    public async Task LogTaskAsync_WhenActionThrows_LogsFailedAndRethrows()
    {
        var act = () => _logger.LogTaskAsync<object>(() => throw new InvalidOperationException("test error"));

        await act.Should().ThrowAsync<InvalidOperationException>();
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("failed"));
    }

    [Fact]
    public async Task LogTaskAsync_WithCancellationToken_Success_LogsStartAndCompleted()
    {
        var cts = new CancellationTokenSource();

        await _logger.LogTaskAsync(() => Task.Delay(10, cts.Token), cts.Token);

        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("started"));
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("completed"));
    }

    [Fact]
    public void LogTask_Sync_Success_LogsStartAndCompleted()
    {
        var result = _logger.LogTask(() => 99);

        result.Should().Be(99);
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("started"));
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("completed"));
    }

    [Fact]
    public void LogTask_Sync_WhenActionThrows_LogsFailedAndRethrows()
    {
        var act = () => _logger.LogTask<object>(() => throw new InvalidOperationException("sync error"));

        act.Should().Throw<InvalidOperationException>();
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("failed"));
    }

    [Fact]
    public void LogTask_VoidAction_Success_LogsStartAndCompleted()
    {
        var executed = false;

        _logger.LogTask(() => { executed = true; });

        executed.Should().BeTrue();
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("started"));
        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("completed"));
    }

    [Fact]
    public async Task LogTaskAsync_WithLogProcessedTime_LogsElapsed()
    {
        await _logger.LogTaskAsync(() => Task.FromResult(1), logProcessedTime: true);

        _logger.Entries.Should().ContainSingle(e => e.Message.Contains("processed time"));
    }

    [Fact]
    public async Task LogTaskAsync_WithoutLogProcessedTime_DoesNotLogElapsed()
    {
        await _logger.LogTaskAsync(() => Task.FromResult(1), logProcessedTime: false);

        _logger.Entries.Should().NotContain(e => e.Message.Contains("processed time"));
    }

    [Fact]
    public async Task LogTaskAsync_WithProcessDescription_UsesDescriptionInLogs()
    {
        const string description = "CustomOperation";

        await _logger.LogTaskAsync(() => Task.FromResult(1), processDescription: description);

        _logger.Entries.Should().Contain(e => e.Message.Contains(description));
    }

    [Fact]
    public async Task LogTaskAsync_WithNullLogger_DoesNotThrow()
    {
        ILogger? nullLogger = null;

        var act = () => nullLogger.LogTaskAsync(() => Task.FromResult(1));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogTaskAsync_WithCustomLogLevel_LogsAtSpecifiedLevel()
    {
        await _logger.LogTaskAsync(() => Task.FromResult(1), logLevel: LogLevel.Debug);

        _logger.Entries.Should().OnlyContain(e => e.Level == LogLevel.Debug || e.Level == LogLevel.Error);
        _logger.Entries.Where(e => e.Level == LogLevel.Debug).Should().NotBeEmpty();
    }

    [Fact]
    public async Task LogTaskAsync_WhenFailed_LogsElapsedAtErrorLevel()
    {
        var act = () => _logger.LogTaskAsync<object>(
            () => throw new InvalidOperationException("fail"),
            logProcessedTime: true);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _logger.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Message.Contains("processed time"));
    }
}
