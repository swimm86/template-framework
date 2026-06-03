// ----------------------------------------------------------------------------------------------
// <copyright file="RetryMiddleware.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Application.Core.Job.Pipeline.Interfaces;

namespace Shared.Application.Core.Job.Pipeline.Middlewares;

/// <summary>
/// In-process retry-middleware. При неуспехе <c>next</c> делает до
/// <see cref="RetryOptions.MaxAttempts"/> попыток, между ними ожидая
/// <see cref="RetryOptions.Delay"/>.
/// </summary>
/// <param name="options">Настройки retry.</param>
/// <param name="logger">Логгер.</param>
public sealed class RetryMiddleware(
    IOptions<RetryOptions> options,
    ILogger<RetryMiddleware> logger)
    : IScheduledJobMiddleware
{
    /// <inheritdoc />
    public async Task InvokeAsync(ScheduledJobContext context, ScheduledJobDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var attempt = 0;
        while (true)
        {
            try
            {
                await next(context);
                return;
            }
            catch (Exception ex) when (++attempt < options.Value.MaxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Job {JobKey} failed (attempt {Attempt}/{MaxAttempts}), retrying in {Delay}.",
                    context.JobKey,
                    attempt,
                    options.Value.MaxAttempts,
                    options.Value.Delay);

                await Task.Delay(options.Value.Delay, context.CancellationToken);
            }
        }
    }
}
