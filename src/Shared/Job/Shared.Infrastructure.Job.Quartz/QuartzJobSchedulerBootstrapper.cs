// ----------------------------------------------------------------------------------------------
// <copyright file="QuartzJobSchedulerBootstrapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Shared.Application.Core.Job.Scheduler;
using Shared.Application.Core.Job.Scheduler.Interfaces;

namespace Shared.Infrastructure.Job.Quartz;

/// <summary>
/// <see cref="IHostedService"/>, который при старте приложения регистрирует все
/// задачи из <see cref="JobSchedulerOptions"/> в Quartz-планировщик и запускает его.
/// </summary>
public sealed class QuartzJobSchedulerBootstrapper(
    JobSchedulerOptions options,
    IJobScheduler scheduler,
    ISchedulerFactory schedulerFactory,
    ILogger<QuartzJobSchedulerBootstrapper> logger)
    : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Registering {Count} job(s) in Quartz scheduler.",
            options.Definitions.Count);

        foreach (var definition in options.Definitions)
        {
            await scheduler.ScheduleAsync(definition, cancellationToken);
        }

        var quartzScheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await quartzScheduler.Start(cancellationToken);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var quartzScheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await quartzScheduler.Shutdown(true, cancellationToken);
    }
}
