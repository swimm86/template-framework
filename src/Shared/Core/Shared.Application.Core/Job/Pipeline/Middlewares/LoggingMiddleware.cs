// ----------------------------------------------------------------------------------------------
// <copyright file="LoggingMiddleware.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Shared.Application.Core.Job.Pipeline.Interfaces;
using Shared.Common.Logging.Extensions;

namespace Shared.Application.Core.Job.Pipeline.Middlewares;

/// <summary>
/// Middleware, логирующий начало/конец выполнения задачи и ошибки.
/// </summary>
/// <param name="logger">Логгер.</param>
public sealed class LoggingMiddleware(
    ILogger<LoggingMiddleware> logger)
    : IScheduledJobMiddleware
{
    /// <inheritdoc />
    public Task InvokeAsync(
        ScheduledJobContext context,
        ScheduledJobDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return logger.LogTaskAsync(
            action: () => next(context),
            CancellationToken.None,
            methodName: context.JobKey);
    }
}
