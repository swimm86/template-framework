// ----------------------------------------------------------------------------------------------
// <copyright file="JobSchedulerBuilder.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Job.Enums;
using Shared.Application.Core.Job.Interfaces;
using Shared.Application.Core.Job.Pipeline;

namespace Shared.Application.Core.Job.Scheduler;

/// <summary>
/// Потокобезопасный построитель для описания фоновых задач в <see cref="JobSchedulerOptions"/>.
/// <para>
/// Поддерживает три формы регистрации:
/// <list type="bullet">
/// <item><description><see cref="AddJob{TJob}(JobSchedule, RetryOptions)"/> — для задач, реализующих <see cref="IScheduledJob"/>.</description></item>
/// <item><description><see cref="AddJob(string, JobSchedule, Func{IServiceProvider, CancellationToken, Task}, RetryOptions)"/> — для задач, заданных делегатом.</description></item>
/// <item><description><see cref="AddCron(string, string, Func{IServiceProvider, CancellationToken, Task}, RetryOptions)"/>, <see cref="AddFlags(string, JobTriggerFlags, TimeSpan, Func{IServiceProvider, CancellationToken, Task}, RetryOptions)"/> — упрощённая запись.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class JobSchedulerBuilder
{
    private readonly List<JobDefinition> _definitions = [];

    /// <summary>
    /// Список собранных определений (только для чтения).
    /// </summary>
    public IReadOnlyList<JobDefinition> Definitions => _definitions;

    /// <summary>
    /// Добавляет фоновую задачу <typeparamref name="TJob"/>.
    /// При выполнении задача будет получена из DI по типу <typeparamref name="TJob"/>.
    /// </summary>
    /// <typeparam name="TJob">Тип фоновой задачи.</typeparam>
    /// <param name="schedule">Расписание запуска.</param>
    /// <param name="retryOptions"><inheritdoc cref="Shared.Application.Core.Job.Pipeline.RetryOptions" path="/summary"/></param>
    /// <returns>Текущий построитель для цепочки вызовов.</returns>
    public JobSchedulerBuilder AddJob<TJob>(
        JobSchedule schedule,
        RetryOptions? retryOptions = null)
        where TJob : class, IScheduledJob
    {
        ArgumentNullException.ThrowIfNull(schedule);

        return AddJob(typeof(TJob), schedule, retryOptions);
    }

    /// <summary>
    /// Добавляет фоновую задачу <typeparamref name="TJob"/> с указанием ключа для keyed-сервиса в DI.
    /// При выполнении задача будет получена из DI по типу <typeparamref name="TJob"/>.
    /// </summary>
    /// <typeparam name="TJob">Тип фоновой задачи.</typeparam>
    /// <param name="serviceKey">Ключ keyed-сервиса.</param>
    /// <param name="schedule">Расписание запуска.</param>
    /// <param name="retryOptions"><inheritdoc cref="Shared.Application.Core.Job.Pipeline.RetryOptions" path="/summary"/></param>
    /// <returns>Текущий построитель для цепочки вызовов.</returns>
    public JobSchedulerBuilder AddJob<TJob>(
        string serviceKey,
        JobSchedule schedule,
        RetryOptions? retryOptions = null)
        where TJob : class, IScheduledJob
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(serviceKey);

        return AddJob(typeof(TJob), serviceKey, schedule, retryOptions);
    }

    /// <summary>
    /// Добавляет фоновую задачу по типу (без generic-синтаксиса).
    /// Используется <c>Type.FullName</c> в качестве ключа.
    /// </summary>
    /// <param name="jobType">Тип фоновой задачи, реализующей <see cref="IScheduledJob"/>.</param>
    /// <param name="schedule">Расписание запуска.</param>
    /// <param name="retryOptions"><inheritdoc cref="Shared.Application.Core.Job.Pipeline.RetryOptions" path="/summary"/></param>
    /// <returns>Текущий построитель для цепочки вызовов.</returns>
    public JobSchedulerBuilder AddJob(
        Type jobType,
        JobSchedule schedule,
        RetryOptions? retryOptions = null)
    {
        ArgumentNullException.ThrowIfNull(jobType);

        if (!typeof(IScheduledJob).IsAssignableFrom(jobType))
        {
            throw new ArgumentException(
                $"Type {jobType.FullName} must implement {nameof(IScheduledJob)}.",
                nameof(jobType));
        }

        ArgumentNullException.ThrowIfNull(schedule);

        // Для классовых джоб Action не используется executor-ом (он резолвит JobType из DI).
        // Передаём null — контракт record-а допускает null для Action при указанном JobType.
        _definitions.Add(new JobDefinition(
            JobKey: jobType.FullName!,
            Action: null,
            Schedule: schedule,
            JobType: jobType,
            RetryOptions: retryOptions));
        return this;
    }

    /// <summary>
    /// Добавляет фоновую задачу с указанием ключа для keyed-сервиса в DI.
    /// Используется, если один тип задачи зарегистрирован как несколько keyed-сервисов
    /// (например, разные ключи кэша для одного типа данных).
    /// </summary>
    /// <param name="jobType">Тип фоновой задачи.</param>
    /// <param name="serviceKey">Ключ keyed-сервиса.</param>
    /// <param name="schedule">Расписание запуска.</param>
    /// <param name="retryOptions"><inheritdoc cref="Shared.Application.Core.Job.Pipeline.RetryOptions" path="/summary"/></param>
    /// <returns>Текущий построитель для цепочки вызовов.</returns>
    public JobSchedulerBuilder AddJob(
        Type jobType,
        string serviceKey,
        JobSchedule schedule,
        RetryOptions? retryOptions = null)
    {
        ArgumentNullException.ThrowIfNull(jobType);

        if (!typeof(IScheduledJob).IsAssignableFrom(jobType))
        {
            throw new ArgumentException(
                $"Type {jobType.FullName} must implement {nameof(IScheduledJob)}.",
                nameof(jobType));
        }

        ArgumentNullException.ThrowIfNull(serviceKey);
        ArgumentNullException.ThrowIfNull(schedule);

        var jobKey = $"{jobType.FullName}#{serviceKey}";
        _definitions.Add(new JobDefinition(
            JobKey: jobKey,
            Action: null,
            Schedule: schedule,
            JobType: jobType,
            ServiceKey: serviceKey,
            RetryOptions: retryOptions));
        return this;
    }

    /// <summary>
    /// Добавляет фоновую задачу, заданную делегатом.
    /// </summary>
    /// <param name="jobKey">Уникальный ключ задачи.</param>
    /// <param name="schedule">Расписание запуска.</param>
    /// <param name="action">
    /// Действие задачи. Получает <see cref="IServiceProvider"/> и <see cref="CancellationToken"/> —
    /// позволяет получать зависимости из DI в момент выполнения.
    /// </param>
    /// <param name="retryOptions"><inheritdoc cref="Shared.Application.Core.Job.Pipeline.RetryOptions" path="/summary"/></param>
    /// <returns>Текущий построитель для цепочки вызовов.</returns>
    public JobSchedulerBuilder AddJob(
        string jobKey,
        JobSchedule schedule,
        Func<IServiceProvider, CancellationToken, Task> action,
        RetryOptions? retryOptions = null)
    {
        ValidateJobKey(jobKey);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(action);

        _definitions.Add(new JobDefinition(jobKey, action, schedule, RetryOptions: retryOptions));
        return this;
    }

    /// <summary>
    /// Упрощённая запись: добавляет фоновую задачу по CRON-выражению.
    /// </summary>
    /// <param name="jobKey">Уникальный ключ задачи.</param>
    /// <param name="cronExpression">CRON-выражение.</param>
    /// <param name="action">Действие задачи.</param>
    /// <param name="retryOptions"><inheritdoc cref="Shared.Application.Core.Job.Pipeline.RetryOptions" path="/summary"/></param>
    /// <returns>Текущий билдер для чейнинга.</returns>
    public JobSchedulerBuilder AddCron(
        string jobKey,
        string cronExpression,
        Func<IServiceProvider, CancellationToken, Task> action,
        RetryOptions? retryOptions = null)
    {
        ValidateJobKey(jobKey);
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            throw new ArgumentNullException(
                nameof(cronExpression),
                "CRON expression cannot be null or empty.");
        }

        ArgumentNullException.ThrowIfNull(action);

        return AddJob(jobKey, new JobSchedule.Cron(cronExpression), action, retryOptions);
    }

    /// <summary>
    /// Упрощённая запись: добавляет фоновую задачу по флагам расписания.
    /// </summary>
    /// <param name="jobKey">Уникальный ключ задачи.</param>
    /// <param name="flags">Флаги расписания.</param>
    /// <param name="specificTime">Время выполнения.</param>
    /// <param name="action">Действие задачи.</param>
    /// <param name="retryOptions"><inheritdoc cref="Shared.Application.Core.Job.Pipeline.RetryOptions" path="/summary"/></param>
    /// <returns>Текущий билдер для чейнинга.</returns>
    public JobSchedulerBuilder AddFlags(
        string jobKey,
        JobTriggerFlags flags,
        TimeSpan specificTime,
        Func<IServiceProvider, CancellationToken, Task> action,
        RetryOptions? retryOptions = null)
    {
        ValidateJobKey(jobKey);
        ArgumentNullException.ThrowIfNull(action);

        return AddJob(jobKey, new JobSchedule.Flags(flags, specificTime), action, retryOptions);
    }

    /// <summary>
    /// Собирает <see cref="JobSchedulerOptions"/> с текущим набором определений.
    /// </summary>
    /// <returns>Новый экземпляр <see cref="JobSchedulerOptions"/>.</returns>
    internal JobSchedulerOptions BuildOptions() => new()
    {
        Definitions = _definitions.ToArray(),
    };

    private static void ValidateJobKey(string jobKey)
    {
        if (string.IsNullOrWhiteSpace(jobKey))
        {
            throw new ArgumentNullException(
                nameof(jobKey),
                "Job key cannot be null or empty.");
        }
    }
}
