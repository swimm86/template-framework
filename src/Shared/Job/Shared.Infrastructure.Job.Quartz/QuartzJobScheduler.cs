// ----------------------------------------------------------------------------------------------
// <copyright file="QuartzJobScheduler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Quartz;
using Shared.Application.Core.Job.Enums;
using Shared.Application.Core.Job.Scheduler;
using Shared.Application.Core.Job.Scheduler.Interfaces;

namespace Shared.Infrastructure.Job.Quartz;

/// <summary>
/// Quartz-реализация <see cref="IJobScheduler"/>. Регистрирует задачи в
/// <see cref="IScheduler"/> по их <see cref="JobDefinition.Schedule"/>.
/// </summary>
public sealed class QuartzJobScheduler(
    ISchedulerFactory schedulerFactory,
    ILogger<QuartzJobScheduler> logger)
    : IJobScheduler
{
    /// <inheritdoc />
    public async Task ScheduleAsync(JobDefinition definition, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var scheduler = await schedulerFactory.GetScheduler(ct);
        var jobKey = new JobKey(definition.JobKey);

        var jobData = new JobDataMap();
        if (definition.JobType is not null)
        {
            var jobTypeName =
                definition.JobType.AssemblyQualifiedName
                ?? throw new InvalidOperationException(
                    $"Type {definition.JobType.FullName} has no {nameof(Type.AssemblyQualifiedName)}.");
            jobData[Constants.JobTypeKey] = jobTypeName;
        }

        if (definition.ServiceKey is not null)
        {
            jobData[Constants.ServiceKeyKey] = definition.ServiceKey;
        }

        if (definition.JobType is null)
        {
            jobData[Constants.ActionDataKey] = definition.Action;
        }

        if (definition.RetryOptions is not null)
        {
            jobData[Constants.RetryOptionsKey] = definition.RetryOptions;
        }

        var job = JobBuilder.Create<QuartzScheduledJobAdapter>()
            .WithIdentity(jobKey)
            .SetJobData(jobData)
            .Build();

        var trigger = BuildTrigger(definition, jobKey);

        await scheduler.ScheduleJob(job, trigger, ct);

        logger.LogInformation(
            "Job {JobKey} registered in Quartz (schedule: {Schedule}).",
            definition.JobKey,
            definition.Schedule.GetType().Name);
    }

    private static ITrigger BuildTrigger(JobDefinition definition, JobKey jobKey)
    {
        var triggerBuilder = TriggerBuilder.Create()
            .ForJob(jobKey)
            .WithIdentity($"{definition.JobKey}.trigger");

        return definition.Schedule switch
        {
            JobSchedule.Cron cron => triggerBuilder
                .WithCronSchedule(cron.Expression)
                .Build(),

            JobSchedule.OnStartup => triggerBuilder
                .StartNow()
                .Build(),

            JobSchedule.Flags flags => BuildFlagsTrigger(triggerBuilder, flags),

            _ => throw new ArgumentOutOfRangeException(
                nameof(definition),
                definition.Schedule,
                "Unknown JobSchedule type."),
        };
    }

    private static ITrigger BuildFlagsTrigger(
        TriggerBuilder triggerBuilder,
        JobSchedule.Flags schedule)
    {
        // Для OnStartup внутри Flags сценарий невозможен, так как OnStartup — отдельный case.
        // Здесь — комбинации Daily/Weekly/.../EveryHour/EveryMinute.
        if (schedule.TriggerFlags == JobTriggerFlags.OnStartup)
        {
            return triggerBuilder.StartNow().Build();
        }

        var scheduleFlags = Enum.GetValues<JobTriggerFlags>()
            .Where(flag => flag != JobTriggerFlags.OnStartup && schedule.TriggerFlags.HasFlag(flag))
            .ToList();

        if (scheduleFlags.Count > 1)
        {
            throw new ArgumentException(
                $"Quartz does not support multiple interval schedules on a single trigger. " +
                $"Specify only one schedule flag (Daily, Weekly, Monthly, EveryHour, EveryMinute) " +
                $"plus optionally OnStartup. Found flags: {schedule.TriggerFlags}.",
                nameof(schedule));
        }

        var baseBuilder = triggerBuilder.StartAt(DateBuilder.DateOf(
            schedule.SpecificTime.Hours,
            schedule.SpecificTime.Minutes,
            0));
        scheduleFlags.ForEach(flag => baseBuilder = ApplyFlagInterval(baseBuilder, flag));
        return baseBuilder.Build();
    }

    private static TriggerBuilder ApplyFlagInterval(
        TriggerBuilder triggerBuilder,
        JobTriggerFlags flag)
    {
        return flag switch
        {
            JobTriggerFlags.EveryMinute => triggerBuilder
                .WithCalendarIntervalSchedule(b => b.WithIntervalInMinutes(1)),
            JobTriggerFlags.EveryHour => triggerBuilder
                .WithCalendarIntervalSchedule(b => b.WithIntervalInHours(1)),
            JobTriggerFlags.Daily => triggerBuilder
                .WithCalendarIntervalSchedule(b => b.WithIntervalInDays(1)),
            JobTriggerFlags.Weekly => triggerBuilder
                .WithCalendarIntervalSchedule(b => b.WithIntervalInWeeks(1)),
            JobTriggerFlags.Monthly => triggerBuilder
                .WithCalendarIntervalSchedule(b => b.WithIntervalInMonths(1)),
            _ => throw new ArgumentOutOfRangeException(nameof(flag), flag, null),
        };
    }
}
