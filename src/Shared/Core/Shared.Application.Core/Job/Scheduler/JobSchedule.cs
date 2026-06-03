// ----------------------------------------------------------------------------------------------
// <copyright file="JobSchedule.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Job.Enums;

namespace Shared.Application.Core.Job.Scheduler;

/// <summary>
/// Расписание запуска фоновой задачи.
/// </summary>
public abstract record JobSchedule
{
    /// <summary>
    /// Запуск по CRON-выражению.
    /// </summary>
    /// <param name="Expression">CRON-выражение, например <c>"0 0/5 * * * ?"</c>.</param>
    public sealed record Cron(string Expression)
        : JobSchedule;

    /// <summary>
    /// Запуск по комбинации флагов расписания с указанием конкретного времени.
    /// </summary>
    /// <param name="TriggerFlags">Флаги расписания (ежедневно/еженедельно/...).</param>
    /// <param name="SpecificTime">Время выполнения задачи.</param>
    public sealed record Flags(JobTriggerFlags TriggerFlags, TimeSpan SpecificTime)
        : JobSchedule;

    /// <summary>
    /// Запуск сразу при старте приложения.
    /// </summary>
    public sealed record OnStartup
        : JobSchedule;
}
