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
/// <remarks>
/// <para>
/// Этот bootstrapper является единственным владельцем жизненного цикла
/// <see cref="IScheduler"/>: <c>AddQuartz</c> в
/// <see cref="QuartzDependencyInjector"/> <c>IHostedService</c> не регистрирует
/// (для этого есть отдельный <c>AddQuartzHostedService</c>), поэтому конфликта
/// старта/остановки здесь нет.
/// </para>
/// <para>
/// Джобы регистрируются <b>до</b> <see cref="IScheduler.Start"/> — благодаря этому
/// триггеры, использующие <c>StartNow()</c> (например, <see cref="JobSchedule.OnStartup"/>),
/// надёжно подхватываются сразу после старта планировщика, а не зависят от гонки
/// между <see cref="IHostedService"/>-ами.
/// </para>
/// </remarks>
/// <param name="options">Список зарегистрированных задач.</param>
/// <param name="scheduler">Планировщик (Quartz-реализация <see cref="IJobScheduler"/>).</param>
/// <param name="schedulerFactory">Фабрика планировщиков Quartz.</param>
/// <param name="logger">Логгер.</param>
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
