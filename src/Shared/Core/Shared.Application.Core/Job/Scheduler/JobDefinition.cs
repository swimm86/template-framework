// ----------------------------------------------------------------------------------------------
// <copyright file="JobDefinition.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Job.Interfaces;

namespace Shared.Application.Core.Job.Scheduler;

/// <summary>
/// Описание фоновой задачи, регистрируемой в планировщике.
/// </summary>
/// <param name="JobKey">Уникальный ключ задачи.</param>
/// <param name="Action">
/// Действие задачи, заданное делегатом. Получает <see cref="IServiceProvider"/> и <see cref="CancellationToken"/>.
/// Для задач с указанным типом (<see cref="JobType"/> != <c>null</c>) должно быть <c>null</c> —
/// executor получит задачу из DI по типу.
/// </param>
/// <param name="Schedule">Расписание запуска.</param>
/// <param name="JobType">
/// Тип фоновой задачи, реализующей <see cref="IScheduledJob"/>, или <c>null</c> для задач, заданных делегатом.
/// </param>
/// <param name="ServiceKey">
/// Ключ для keyed-сервиса в DI — используется, если один <see cref="JobType"/>
/// зарегистрирован как несколько keyed-сервисов (например, несколько
/// фоновых задач обновления кэша для разных ключей).
/// </param>
public sealed record JobDefinition(
    string JobKey,
    Func<IServiceProvider, CancellationToken, Task>? Action,
    JobSchedule Schedule,
    Type? JobType = null,
    string? ServiceKey = null)
{
    /// <summary>
    /// Ключ в Quartz <c>JobDataMap</c> для лямбда-действия.
    /// Используется <c>QuartzScheduledJobAdapter</c> для восстановления делегата при выполнении.
    /// </summary>
    public const string ActionDataKey = "JobAction";
}
