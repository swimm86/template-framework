// ----------------------------------------------------------------------------------------------
// <copyright file="QuartzScheduledJobAdapter.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Quartz;
using Shared.Application.Core.Job.Interfaces;
using Shared.Application.Core.Job.Pipeline;
using Shared.Application.Core.Job.Pipeline.Interfaces;

namespace Shared.Infrastructure.Job.Quartz;

/// <summary>
/// Quartz-обёртка над <see cref="IScheduledJob"/>. Используется <c><see cref="QuartzJobScheduler"/></c>
/// при создании <see cref="IJobDetail"/>.
/// </summary>
/// <remarks>
/// Эта адаптер-обёртка <b>внутренняя</b>: пользовательский код не должен реализовывать
/// <see cref="IJob"/> — он реализует <see cref="IScheduledJob"/>. Quartz-типы остаются
/// за пределами Application.Core.
/// </remarks>
internal sealed class QuartzScheduledJobAdapter(
    IServiceProvider serviceProvider,
    IScheduledJobExecutor executor,
    ILogger<QuartzScheduledJobAdapter> logger)
    : IJob
{
    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        var jobKey = context.JobDetail.Key.Name;
        var cancellationToken = context.CancellationToken;

        var jobType = context.JobDetail.JobDataMap[Constants.JobTypeKey] is not string jobTypeName
            ? null
            : Type.GetType(jobTypeName, throwOnError: false);
        var action = context.JobDetail.JobDataMap[Constants.ActionDataKey] as Func<IServiceProvider, CancellationToken, Task>;

        if (jobType is null && action is null)
        {
            logger.LogError(
                "Job '{JobKey}': {JobDataMap} not contains action for current keyed job type.",
                jobKey,
                nameof(IJobDetail.JobDataMap));
            return;
        }

        var serviceKey = context.JobDetail.JobDataMap[Constants.ServiceKeyKey] as string;
        var retryOptions = context.JobDetail.JobDataMap[Constants.RetryOptionsKey] as RetryOptions;
        var ctx = new ScheduledJobContext(jobKey, serviceProvider, cancellationToken)
        {
            JobType = jobType,
            ServiceKey = serviceKey,
            Action = action,
            RetryOptions = retryOptions,
        };

        await executor.ExecuteAsync(ctx);
    }
}
