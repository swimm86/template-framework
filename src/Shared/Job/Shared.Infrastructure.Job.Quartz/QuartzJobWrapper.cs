// ----------------------------------------------------------------------------------------------
// <copyright file="QuartzJobWrapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Triggers;
using Shared.Application.Core.CorrelationId;

namespace Shared.Infrastructure.Job.Quartz;

/// <summary>
/// Обертка для <see cref="IJob"/>.
/// </summary>
/// <param name="serviceProvider">Экземпляр <see cref="IServiceProvider"/> для работы с DI.</param>
/// <param name="logger">Экземпляр <see cref="ILogger{QuartzJobWrapper}"/> для работы с логированием.</param>
public class QuartzJobWrapper(
    IServiceProvider serviceProvider,
    ILogger<QuartzJobWrapper> logger)
    : IJob
{
    /// <summary>
    /// Ключ задачи в <see cref="JobDataMap"/>.
    /// </summary>
    public const string JobActionKey = "JobAction";

    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var correlationIdCreated = JobCorrelationContext.TrySetCorrelationId();
        if (!correlationIdCreated)
        {
            logger.LogWarning("Correlation ID already set for job {JobKey}", context.JobDetail.Key);
        }

        try
        {
            await ProcessAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            // Запускаем повторно через 5 минут.
            var retryTrigger = new SimpleTriggerImpl(Guid.NewGuid().ToString())
            {
                Description = "RetryTrigger",
                RepeatCount = 0,
                JobKey = context.JobDetail.Key,
                StartTimeUtc = DateBuilder.NextGivenMinuteDate(DateTime.Now, 5),
            };
            await context.Scheduler.ScheduleJob(retryTrigger, cancellationToken);
            throw new JobExecutionException(ex, false);
        }
        finally
        {
            if (correlationIdCreated)
            {
                JobCorrelationContext.ClearCorrelationId();
            }
        }
    }

    /// <summary>
    /// Обрабатывает задачу.
    /// </summary>
    /// <param name="context"><see cref="IJobExecutionContext"/>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    protected virtual async Task ProcessAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Job {JobKey} is executing.", context.JobDetail.Key);
        await (context.JobDetail.JobDataMap[JobActionKey] as Func<IServiceProvider, CancellationToken, Task>)!
            .Invoke(serviceProvider, cancellationToken);
        logger.LogInformation("Job {JobKey} is completed.", context.JobDetail.Key);
    }
}
