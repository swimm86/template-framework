// ----------------------------------------------------------------------------------------------
// <copyright file="CorrelationIdMiddlewareTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.CorrelationId;
using Shared.Application.Core.Job.Pipeline;
using Shared.Application.Core.Job.Pipeline.Middlewares;
using Shared.Testing.Doubles.Logging;

namespace Shared.Application.Core.Tests;

/// <summary>
/// Тесты <see cref="CorrelationIdMiddleware"/>: установка/очистка correlation-id.
/// </summary>
public sealed class CorrelationIdMiddlewareTests
{
    public CorrelationIdMiddlewareTests()
    {
        JobCorrelationContext.ClearCorrelationId();
    }

    /// <summary>
    /// При выполнении correlation-id устанавливается и очищается.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_SetsAndClearsCorrelationId()
    {
        // Arrange
        var logger = new FakeLogger();
        var middleware = new CorrelationIdMiddleware(new FakeLogger<CorrelationIdMiddleware>(logger));
        var sp = new ServiceCollection().BuildServiceProvider();
        var ctx = new ScheduledJobContext("k", sp, CancellationToken.None)
        {
            Action = (_, _) =>
            {
                JobCorrelationContext.GetCorrelationId().Should().NotBeNull();
                return Task.CompletedTask;
            },
        };

        // Act
        await middleware.InvokeAsync(ctx, c => c.Action!(sp, c.CancellationToken));

        // Assert
        JobCorrelationContext.GetCorrelationId().Should().BeNull();
    }

    /// <summary>
    /// Если correlation-id уже был выставлен — middleware не очищает его.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_PreExistingCorrelationId_DoesNotClear()
    {
        // Arrange
        var preExisting = Guid.NewGuid();
        JobCorrelationContext.TrySetCorrelationId().Should().BeTrue();
        var preExistingId = JobCorrelationContext.GetCorrelationId();
        preExistingId.Should().NotBeNull();

        var logger = new FakeLogger();
        var middleware = new CorrelationIdMiddleware(new FakeLogger<CorrelationIdMiddleware>(logger));
        var sp = new ServiceCollection().BuildServiceProvider();
        var ctx = new ScheduledJobContext("k", sp, CancellationToken.None)
        {
            Action = (_, _) => Task.CompletedTask,
        };

        // Act
        await middleware.InvokeAsync(ctx, c => c.Action!(sp, c.CancellationToken));

        // Assert
        JobCorrelationContext.GetCorrelationId().Should().Be(preExistingId);
    }
}
