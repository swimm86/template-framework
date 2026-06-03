// ----------------------------------------------------------------------------------------------
// <copyright file="HangfireJobScheduler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Hangfire;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Job.Enums;
using Shared.Application.Core.Job.Scheduler;
using Shared.Application.Core.Job.Scheduler.Interfaces;

namespace Shared.Infrastructure.Job.Hangfire;

/// <summary>
/// Hangfire-реализация <see cref="IJobScheduler"/>. Регистрирует задачи в Hangfire
/// по их <see cref="JobDefinition.Schedule"/>.
/// </summary>
/// <remarks>
/// <para>
/// Планирование ВСЕГДА идёт через <see cref="HangfireScheduledJobAdapter"/>, который
/// инкапсулирует детали сериализации (<c>MethodCall</c>-выражение) и предоставляет
/// фабрику <see cref="HangfireScheduledJobAdapter.CreateHangfireJob"/>.
/// <see cref="IRecurringJobManager"/> и <see cref="IBackgroundJobClient"/> — это Hangfire-интерфейсы,
/// которые регистрируются автоматически при <c>AddHangfire(...).AddHangfireServer()</c>.
/// </para>
/// <para>
/// <b>Задачи, заданные делегатом (без <see cref="JobDefinition.JobType"/>) не поддерживаются</b> в Hangfire:
/// замыкание на <c>Func</c>-переменную нельзя сериализовать. Для cron/OnStartup в Hangfire
/// используйте <c>AddJob&lt;MyClassJob&gt;(...)</c> — задачи, реализующие интерфейс фоновой задачи.
/// </para>
/// </remarks>
public sealed class HangfireJobScheduler(
    IRecurringJobManager recurringJobManager,
    IBackgroundJobClient backgroundJobClient,
    ILogger<HangfireJobScheduler> logger)
    : IJobScheduler
{
    /// <inheritdoc />
    public Task ScheduleAsync(JobDefinition definition, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.JobType is null)
        {
            throw new NotSupportedException(
                $"Lambda job '{definition.JobKey}' is not supported in Hangfire. " +
                "Use class jobs via opts.AddJob<TClassJob>(...).");
        }

        var jobKey = definition.JobKey;
        var typeName =
            definition.JobType!.AssemblyQualifiedName
            ?? throw new InvalidOperationException(
                $"Type {definition.JobType.FullName} has no {nameof(Type.AssemblyQualifiedName)}.");
        var serviceKey = definition.ServiceKey;
        switch (definition.Schedule)
        {
            case JobSchedule.Cron cron:
                ScheduleCron(jobKey, typeName, serviceKey, cron);
                break;
            case JobSchedule.OnStartup:
                ScheduleOnStartup(jobKey, typeName, serviceKey);
                break;
            case JobSchedule.Flags flags:
                ScheduleFlags(jobKey, typeName, serviceKey, flags);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(definition),
                    definition.Schedule,
                    $"Unknown {nameof(JobSchedule)} type.");
        }

        return Task.CompletedTask;
    }

    private static string? BuildCronFromFlag(JobTriggerFlags flag, TimeSpan specificTime)
    {
        var minute = specificTime.Minutes;
        var hour = specificTime.Hours;

        return flag switch
        {
            JobTriggerFlags.EveryMinute => "* * * * *",
            JobTriggerFlags.EveryHour => $"{minute} * * * *",
            JobTriggerFlags.Daily => $"{minute} {hour} * * *",
            JobTriggerFlags.Weekly => $"{minute} {hour} * * 1",
            JobTriggerFlags.Monthly => $"{minute} {hour} 1 * *",
            _ => null,
        };
    }

    private void ScheduleCron(
        string? jobKey,
        string typeName,
        string? serviceKey,
        JobSchedule.Cron schedule)
    {
        logger.LogInformation(
            "Job {JobKey} is being registered as {RecurringJobName} (cron: {Cron}).",
            jobKey,
            nameof(RecurringJob),
            schedule.Expression);

        var job = HangfireScheduledJobAdapter.CreateHangfireJob(typeName, serviceKey);
        recurringJobManager.AddOrUpdate(jobKey, job, schedule.Expression, new RecurringJobOptions());
    }

    private void ScheduleOnStartup(string? jobKey, string typeName, string? serviceKey)
    {
        logger.LogInformation(
            "Job {JobKey} is being registered as {BackgroundJobName} (OnStartup).",
            jobKey,
            nameof(BackgroundJob));

        var job = HangfireScheduledJobAdapter.CreateHangfireJob(typeName, serviceKey);
        backgroundJobClient.Create(job, new ScheduledState(TimeSpan.Zero));
    }

    [SuppressMessage(
        "Performance",
        "CA1822:Mark members as static",
        Justification = "Uses the instance field _logger.")]
    private void ScheduleFlags(string? jobKey, string typeName, string? serviceKey, JobSchedule.Flags schedule)
    {
        // OnStartup обрабатывается отдельно: он создаёт fire-and-forget background job,
        // а не recurring. Мы проверяем его ДО цикла cron-флагов, чтобы он также срабатывал
        // в комбинациях (например, Daily | OnStartup — и recurring, и background).
        if (schedule.TriggerFlags.HasFlag(JobTriggerFlags.OnStartup))
        {
            logger.LogInformation(
                "Job {JobKey} is being registered as {BackgroundJobName} (OnStartup via Flags).",
                jobKey,
                nameof(BackgroundJob));

            var startupJob = HangfireScheduledJobAdapter.CreateHangfireJob(typeName, serviceKey);
            backgroundJobClient.Create(startupJob, new ScheduledState(TimeSpan.Zero));
        }

        var specificTime = schedule.SpecificTime;
        foreach (var flag in Enum.GetValues<JobTriggerFlags>())
        {
            if (flag == JobTriggerFlags.OnStartup || !schedule.TriggerFlags.HasFlag(flag))
            {
                continue;
            }

            var cron = BuildCronFromFlag(flag, specificTime);
            if (cron is null)
            {
                logger.LogWarning(
                    "Job {JobKey}: flag {Flag} has no Hangfire cron mapping — skipped.",
                    jobKey,
                    flag);
                continue;
            }

            logger.LogInformation(
                "Job {JobKey} is being registered as {RecurringJobName} (cron: {Cron}, flag: {Flag}).",
                jobKey,
                nameof(RecurringJob),
                cron,
                flag);

            var flagJob = HangfireScheduledJobAdapter.CreateHangfireJob(typeName, serviceKey);
            recurringJobManager.AddOrUpdate($"{jobKey}#{flag}", flagJob, cron, new RecurringJobOptions());
        }
    }
}
