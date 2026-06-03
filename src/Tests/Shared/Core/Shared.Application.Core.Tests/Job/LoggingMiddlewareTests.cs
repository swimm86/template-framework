// ----------------------------------------------------------------------------------------------
// <copyright file="LoggingMiddlewareTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Job.Pipeline;
using Shared.Application.Core.Job.Pipeline.Middlewares;
using Shared.Testing.Doubles.Logging;

namespace Shared.Application.Core.Tests.Job;

/// <summary>
/// Тесты <see cref="LoggingMiddleware"/>: happy path и проброс исключения.
/// </summary>
public sealed class LoggingMiddlewareTests
{
    /// <summary>
    /// При успешном выполнении логируется start и completed.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_HappyPath_LogsStartAndComplete()
    {
        // Arrange
        var logger = new FakeLogger();
        var middleware = new LoggingMiddleware(new FakeLogger<LoggingMiddleware>(logger));
        var sp = new ServiceCollection().BuildServiceProvider();
        var ctx = new ScheduledJobContext("k", sp, CancellationToken.None)
        {
            Action = (_, _) => Task.CompletedTask,
        };

        // Act
        await middleware.InvokeAsync(ctx, c => c.Action!(sp, c.CancellationToken));

        // Assert
        logger.Entries.Select(e => e.Message).Should().Contain(m => m.Contains("started"));
        logger.Entries.Select(e => e.Message).Should().Contain(m => m.Contains("completed"));
    }

    /// <summary>
    /// При исключении логируется failed и исключение пробрасывается.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ExceptionThrown_LogsFailedAndRethrows()
    {
        // Arrange
        var logger = new FakeLogger();
        var middleware = new LoggingMiddleware(new FakeLogger<LoggingMiddleware>(logger));
        var sp = new ServiceCollection().BuildServiceProvider();
        var ctx = new ScheduledJobContext("k", sp, CancellationToken.None);

        ScheduledJobDelegate next = _ => throw new InvalidOperationException("boom");

        // Act
        var act = () => middleware.InvokeAsync(ctx, next);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        logger.Entries.Select(e => e.Message).Should().Contain(m => m.Contains("failed"));
    }
}
