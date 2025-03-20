// ----------------------------------------------------------------------------------------------
// <copyright file="QuartzJobWrapper.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Triggers;

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
        try
        {
            await ProcessAsync(context);
        }
        catch (Exception ex)
        {
            // Запускам пвторно через 5 минут.
            var retryTrigger = new SimpleTriggerImpl(Guid.NewGuid().ToString())
            {
                Description = "RetryTrigger",
                RepeatCount = 0,
                JobKey = context.JobDetail.Key,
                StartTimeUtc = DateBuilder.NextGivenMinuteDate(DateTime.Now, 5),
            };
            await context.Scheduler.ScheduleJob(retryTrigger);
            throw new JobExecutionException(ex, false);
        }
    }

    /// <summary>
    /// Обрабатывает задачу.
    /// </summary>
    /// <param name="context"><see cref="IJobExecutionContext"/>.</param>
    /// <returns>Результат выполнения асинхронной опарации.</returns>
    protected virtual async Task ProcessAsync(IJobExecutionContext context)
    {
        logger.LogInformation($"Job {context.JobDetail.Key} is executing.");
        await (context.JobDetail.JobDataMap[JobActionKey] as Func<IServiceProvider, Task>)!
            .Invoke(serviceProvider);
        logger.LogInformation($"Job {context.JobDetail.Key} is completed.");
    }
}
