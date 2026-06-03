// ----------------------------------------------------------------------------------------------
// <copyright file="ScheduledJobExecutor.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Job.Interfaces;
using Shared.Application.Core.Job.Pipeline.Interfaces;

namespace Shared.Application.Core.Job.Pipeline;

/// <summary>
/// Стандартная реализация <see cref="IScheduledJobExecutor"/>.
/// <para>
/// Сборка конвейера обработки: первый зарегистрированный посредник в DI — самый внешний.
/// Терминальный делегат получает фоновую задачу из <see cref="IServiceProvider"/> (если указан
/// <c>JobType</c>), либо вызывает <see cref="ScheduledJobContext.Action"/>.
/// </para>
/// </summary>
/// <param name="middlewares">Middleware-ы, упорядоченные в DI.</param>
internal sealed class ScheduledJobExecutor(
    IEnumerable<IScheduledJobMiddleware> middlewares)
    : IScheduledJobExecutor
{
    private readonly IReadOnlyList<IScheduledJobMiddleware> _middlewares = middlewares.ToArray();

    /// <inheritdoc />
    public Task ExecuteAsync(ScheduledJobContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Reverse + Aggregate: первый middleware в DI = самый внешний.
        var pipeline = _middlewares
            .Reverse()
            .Aggregate(
                (ScheduledJobDelegate)TerminalAsync,
                (next, mw) => ctx => mw.InvokeAsync(ctx, next));

        return pipeline(context);
    }

    private Task TerminalAsync(ScheduledJobContext ctx)
    {
        if (ctx.JobType is not null)
        {
            IScheduledJob job;
            if (ctx.ServiceKey is not null)
            {
                var keyed = ctx.ServiceProvider.GetKeyedService<IScheduledJob>(ctx.ServiceKey);
                job =
                    keyed ??
                          throw new InvalidOperationException(
                              $"Job '{ctx.JobKey}': keyed service {nameof(IScheduledJob)} " +
                              $"with key '{ctx.ServiceKey}' (type {ctx.JobType.FullName}) " +
                              $"is not registered in DI.");
            }
            else
            {
                job = (IScheduledJob)ctx.ServiceProvider.GetRequiredService(ctx.JobType);
            }

            return job.ExecuteAsync(ctx.CancellationToken);
        }

        if (ctx.Action is null)
        {
                throw new InvalidOperationException(
                    $"Job {ctx.JobKey} has neither {nameof(ScheduledJobContext.JobType)} " +
                    $"nor {nameof(ScheduledJobContext.Action)} — nothing to execute.");
        }

        return ctx.Action(ctx.ServiceProvider, ctx.CancellationToken);
    }
}
