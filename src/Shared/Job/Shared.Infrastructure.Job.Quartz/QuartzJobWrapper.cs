// ----------------------------------------------------------------------------------------------
// <copyright file="QuartzJobWrapper.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Quartz;

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
        logger.LogInformation($"Job {context.JobDetail.Key} is executing.");
        await (context.JobDetail.JobDataMap[JobActionKey] as Func<IServiceProvider, Task>)!
            .Invoke(serviceProvider);
        logger.LogInformation($"Job {context.JobDetail.Key} is completed.");
    }
}