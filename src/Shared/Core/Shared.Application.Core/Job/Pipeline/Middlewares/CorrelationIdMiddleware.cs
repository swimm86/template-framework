// ----------------------------------------------------------------------------------------------
// <copyright file="CorrelationIdMiddleware.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Shared.Application.Core.CorrelationId;
using Shared.Application.Core.Job.Pipeline.Interfaces;

namespace Shared.Application.Core.Job.Pipeline.Middlewares;

/// <summary>
/// Middleware, устанавливающий <see cref="JobCorrelationContext"/> на время выполнения задачи.
/// </summary>
/// <param name="logger">Логгер.</param>
public sealed class CorrelationIdMiddleware(
    ILogger<CorrelationIdMiddleware> logger)
    : IScheduledJobMiddleware
{
    /// <inheritdoc />
    public async Task InvokeAsync(ScheduledJobContext context, ScheduledJobDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var correlationIdCreated = JobCorrelationContext.TrySetCorrelationId();
        if (!correlationIdCreated)
        {
            logger.LogWarning(
                "Correlation ID already initialized before job '{jobKey}' and not cleared.",
                context.JobKey);
        }

        try
        {
            await next(context);
        }
        finally
        {
            if (correlationIdCreated)
            {
                JobCorrelationContext.ClearCorrelationId();
            }
        }
    }
}
