// ----------------------------------------------------------------------------------------------
// <copyright file="IJobScheduler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Job.Scheduler.Interfaces;

/// <summary>
/// Runtime API планировщика. Используется для динамической регистрации задач
/// (например, из админ-эндпоинтов), помимо bootstrap-конфигурации в <see cref="JobSchedulerOptions"/>.
/// </summary>
/// <remarks>
/// (<see cref="JobDefinition.JobType"/> = <c>null</c>) из-за невозможности сериализации делегатов.
/// </remarks>
public interface IJobScheduler
{
    /// <summary>
    /// Регистрирует задачу в планировщике.
    /// </summary>
    /// <param name="definition">Описание задачи.</param>
    /// <param name="ct"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task ScheduleAsync(JobDefinition definition, CancellationToken ct = default);
}
