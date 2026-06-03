// ----------------------------------------------------------------------------------------------
// <copyright file="JobSchedulerOptions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Hosting;
using Shared.Application.Core.Job.Scheduler.Interfaces;

namespace Shared.Application.Core.Job.Scheduler;

/// <summary>
/// Контейнер для начальной (bootstrap) конфигурации планировщика.
/// Содержит список зарегистрированных фоновых задач, которые <c><see cref="IHostedService"/></c> адаптера
/// получит из DI при старте приложения и передаст в <see cref="IJobScheduler.ScheduleAsync"/>.
/// </summary>
public sealed class JobSchedulerOptions
{
    /// <summary>
    /// Список определений задач.
    /// </summary>
    public IReadOnlyList<JobDefinition> Definitions { get; init; } = [];
}
