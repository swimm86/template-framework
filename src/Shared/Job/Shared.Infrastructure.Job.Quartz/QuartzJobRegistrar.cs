// ----------------------------------------------------------------------------------------------
// <copyright file="QuartzJobRegistrar.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Shared.Application.Core.Cache;
using Shared.Application.Core.Job;
using Shared.Common.Extensions;

namespace Shared.Infrastructure.Job.Quartz;

/// <summary>
/// Регистратор задач для планировщика Quartz.
/// Позволяет регистрировать задачи с различными типами триггеров (CRON-выражения, временные интервалы и т.д.).
/// </summary>
/// <remarks>
/// <para>
/// Класс предоставляет два основных метода:
/// <list type="bullet">
/// <item>
/// <description><see cref="RegisterJob(IServiceCollection,string,string,Func{IServiceProvider,CancellationToken,Task})"/>.</description>
/// </item>
/// <item>
/// <description><see cref="RegisterJob{TJob}(IServiceCollection,string)"/>.</description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Для работы с задачами используется обертка <see cref="QuartzJobWrapper"/>, которая позволяет передавать действия
/// через <see cref="JobDataMap"/>.
/// </para>
/// <example>
/// Пример регистрации задачи с CRON-выражением:
/// <code>
/// serviceCollection.RegisterJob(
///     jobKey: "MyJob",
///     cronExpression: "0 0/5 * * * ?", // Каждые 5 минут
///     job: (_, ct) => MyTask(ct));
/// </code>
/// </example>
/// </remarks>
public static class QuartzJobRegistrar
{
    /// <summary>
    /// Регистрирует задачу с использованием CRON-выражения.
    /// </summary>
    /// <param name="serviceCollection">Экземпляр <see cref="IServiceCollection"/> для работы с ним.</param>
    /// <param name="jobKey">
    /// Уникальный ключ задачи. Используется для идентификации задачи в планировщике.
    /// </param>
    /// <param name="cronExpression">
    /// CRON-выражение, определяющее расписание выполнения задачи.
    /// Например, "0 0/5 * * * ?" означает выполнение каждые 5 минут.
    /// </param>
    /// <param name="job">
    /// Действие, которое будет выполняться при запуске задачи.
    /// </param>
    /// <returns>Экземпляр <see cref="IServiceCollection"/> для работы с ним.</returns>
    public static IServiceCollection RegisterJob(
        this IServiceCollection serviceCollection,
        string jobKey,
        string cronExpression,
        Func<IServiceProvider, CancellationToken, Task> job)
    {
        ValidateParameters(jobKey, true, cronExpression, job);

        return serviceCollection
            .ConfigureQuartz(q =>
            {
                q.AddJob<QuartzJobWrapper>(opt => CreateJobDetail(opt, jobKey, job));
                q.AddTrigger(opt => AddTrigger(opt, jobKey, cronExpression));
            });
    }

    /// <summary>
    /// Регистрирует задачу типа <see cref="TJob"/> с использованием CRON-выражения.
    /// </summary>
    /// <typeparam name="TJob">Тип задачи.</typeparam>
    /// <param name="serviceCollection">Экземпляр <see cref="IServiceCollection"/> для работы с ним.</param>
    /// <param name="cronExpression">
    /// CRON-выражение, определяющее расписание выполнения задачи.
    /// Например, "0 0/5 * * * ?" означает выполнение каждые 5 минут.
    /// </param>
    /// <returns>Экземпляр <see cref="IServiceCollection"/> для работы с ним.</returns>
    public static IServiceCollection RegisterJob<TJob>(
        this IServiceCollection serviceCollection,
        string cronExpression)
        where TJob : IJob
    {
        var jobKey = typeof(TJob).FullName!;
        ValidateParameters(jobKey, false, cronExpression);

        return serviceCollection
            .ConfigureQuartz(q =>
            {
                q.AddJob<TJob>(opt => CreateJobDetail(opt, jobKey));
                q.AddTrigger(opt => AddTrigger(opt, jobKey, cronExpression));
            });
    }

    /// <summary>
    /// Регистрирует задачу с использованием флагов триггеров.
    /// </summary>
    /// <param name="serviceCollection">Экземпляр <see cref="IServiceCollection"/> для работы с ним.</param>
    /// <param name="jobKey">
    /// Уникальный ключ задачи. Используется для идентификации задачи в планировщике.
    /// </param>
    /// <param name="trigger">
    /// Флаги триггеров, определяющие расписание выполнения задачи.
    /// Например, <see cref="JobTriggerFlags.Daily"/> для ежедневного выполнения.
    /// </param>
    /// <param name="job">
    /// Действие, которое будет выполняться при запуске задачи.
    /// </param>
    /// <param name="specificTime">
    /// Время выполнения задачи. Используется для триггеров, таких как ежедневное, еженедельное или ежемесячное выполнение.
    /// </param>
    /// <returns>Экземпляр <see cref="IServiceCollection"/> для работы с ним.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Выбрасывается, если указан недопустимый флаг триггера.
    /// </exception>
    public static IServiceCollection RegisterJob(
        this IServiceCollection serviceCollection,
        string jobKey,
        JobTriggerFlags trigger,
        Func<IServiceProvider, CancellationToken, Task> job,
        TimeSpan specificTime)
    {
        ValidateParameters(jobKey, true, job: job);

        return serviceCollection
            .ConfigureQuartz(q =>
            {
                q.AddJob<QuartzJobWrapper>(opt => CreateJobDetail(opt, jobKey, job));
                Enum.GetValues<JobTriggerFlags>()
                    .Where(flag => trigger.HasFlag(flag))
                    .ForEach(flag => CreateTriggerForFlag(q, flag, jobKey, specificTime));
            });
    }

    /// <summary>
    /// Регистрирует задачу типа <see cref="TJob"/> с использованием флагов триггеров.
    /// </summary>
    /// <typeparam name="TJob">Тип задачи.</typeparam>
    /// <param name="serviceCollection">Экземпляр <see cref="IServiceCollection"/> для работы с ним.</param>
    /// <param name="trigger">
    /// Флаги триггеров, определяющие расписание выполнения задачи.
    /// Например, <see cref="JobTriggerFlags.Daily"/> для ежедневного выполнения.
    /// </param>
    /// <param name="specificTime">
    /// Время выполнения задачи. Используется для триггеров, таких как ежедневное, еженедельное или ежемесячное выполнение.
    /// </param>
    /// <returns>Экземпляр <see cref="IServiceCollection"/> для работы с ним.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Выбрасывается, если указан недопустимый флаг триггера.
    /// </exception>
    public static IServiceCollection RegisterJob<TJob>(
        this IServiceCollection serviceCollection,
        JobTriggerFlags trigger,
        TimeSpan specificTime)
        where TJob : IJob
    {
        var jobKey = typeof(TJob).FullName!;
        ValidateParameters(jobKey, false);

        return serviceCollection
            .ConfigureQuartz(q =>
            {
                q.AddJob<TJob>(opt => CreateJobDetail(opt, jobKey));
                Enum.GetValues<JobTriggerFlags>()
                    .Where(flag => trigger.HasFlag(flag))
                    .ForEach(flag => CreateTriggerForFlag(q, flag, jobKey, specificTime));
            });
    }

    /// <summary>
    /// Регистрирует кэш с использованием CRON-выражения.
    /// </summary>
    /// <typeparam name="TData">Тип кэша.</typeparam>
    /// <param name="serviceCollection">Экземпляр <see cref="IServiceCollection"/> для работы с ним.</param>
    /// <param name="cacheKey">
    /// Уникальный ключ кэша. Используется для идентификации кэша, а также задачи в планировщике.
    /// Ключ задачи будет: "{cacheKey}.job".
    /// </param>
    /// <param name="cronExpression">
    /// CRON-выражение, определяющее расписание выполнения задачи.
    /// Например, "0 0/5 * * * ?" означает выполнение каждые 5 минут.
    /// </param>
    /// <param name="getOrCreateCacheFunc">Функция, которая возвращает кэш.</param>
    /// <remarks>
    /// <example>
    /// Пример регистрации задачи с CRON-выражением:
    /// <code>
    /// serviceCollection
    ///    .RegisterCacheJob(
    ///       "test",
    ///       "0 * * * * ?",
    ///       _ => Task.FromResult(Enumerable.Range(0, 10).Select(i => $"item {i}").ToArray()))
    /// </code>
    /// </example>
    /// </remarks>
    /// <returns>Экземпляр <see cref="IServiceCollection"/> для работы с ним.</returns>
    public static IServiceCollection RegisterCacheJob<TData>(
        this IServiceCollection serviceCollection,
        string cacheKey,
        string cronExpression,
        Func<IServiceProvider, Task<TData>> getOrCreateCacheFunc)
    {
        const string jobPrefix = "job";
        return serviceCollection
            .RegisterCacheService(cacheKey, getOrCreateCacheFunc)
            .RegisterJob(
                $"{cacheKey}.{jobPrefix}",
                cronExpression,
                (serviceProvider, _) =>
                {
                    var cacheService = serviceProvider.GetCacheService<TData>(cacheKey);
                    return cacheService.UpdateCacheAsync();
                });
    }

    /// <summary>
    /// Регистрирует задачу с использованием флагов триггеров.
    /// </summary>
    /// <typeparam name="TData">Тип кэша.</typeparam>
    /// <param name="serviceCollection">Экземпляр <see cref="IServiceCollection"/> для работы с ним.</param>
    /// <param name="cacheKey">
    /// Уникальный ключ кэша. Используется для идентификации кэша, а также задачи в планировщике.
    /// Ключ задачи будет: "{cacheKey}.job".
    /// </param>
    /// <param name="trigger">
    /// Флаги триггеров, определяющие расписание выполнения задачи.
    /// Например, <see cref="JobTriggerFlags.Daily"/> для ежедневного выполнения.
    /// </param>
    /// <param name="specificTime">
    /// Время выполнения задачи. Используется для триггеров, таких как ежедневное, еженедельное или ежемесячное выполнение.
    /// </param>
    /// <param name="getOrCreateCacheFunc">Функция, которая возвращает кэш.</param>
    /// <remarks>
    /// <example>
    /// Пример регистрации задачи с использованием флагов триггеров:
    /// <code>
    /// serviceCollection
    ///    .RegisterCacheJob(
    ///       "test",
    ///       JobTriggerFlags.Daily | JobTriggerFlags.EveryMinute,
    ///       new TimeSpan(0, 0, 0, 0),
    ///       _ => Task.FromResult(Enumerable.Range(0, 10).Select(i => $"item {i}").ToArray()))
    /// </code>
    /// </example>
    /// </remarks>
    /// <returns>Экземпляр <see cref="IServiceCollection"/> для работы с ним.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Выбрасывается, если указан недопустимый флаг триггера.
    /// </exception>
    public static IServiceCollection RegisterCacheJob<TData>(
        this IServiceCollection serviceCollection,
        string cacheKey,
        JobTriggerFlags trigger,
        TimeSpan specificTime,
        Func<IServiceProvider, Task<TData>> getOrCreateCacheFunc)
    {
        return serviceCollection
            .RegisterCacheService(cacheKey, getOrCreateCacheFunc)
            .RegisterJob(
                $"{cacheKey}.job",
                trigger,
                (serviceProvider, _) =>
                {
                    var cacheService = serviceProvider.GetCacheService<TData>(cacheKey);
                    return cacheService.UpdateCacheAsync();
                },
                specificTime);
    }

    /// <summary>
    /// Создает детали задачи.
    /// </summary>
    private static void CreateJobDetail(
        IJobConfigurator configurator,
        string jobKey,
        Func<IServiceProvider, CancellationToken, Task>? job = null)
    {
        var builder = configurator
            .WithIdentity(jobKey);
        if (job is not null)
        {
            builder.SetJobData(new JobDataMap { { QuartzJobWrapper.JobActionKey, job } });
        }

        builder.Build();
    }

    /// <summary>
    /// Создает триггер на основе CRON-выражения.
    /// </summary>
    private static void AddTrigger(
        ITriggerConfigurator configurator,
        string jobKey,
        string cronExpression)
    {
        configurator
            .ForJob(jobKey)
            .WithIdentity($"{jobKey}.trigger")
            .WithCronSchedule(cronExpression);
    }

    /// <summary>
    /// Создает триггер на основе флага.
    /// </summary>
    private static void CreateTriggerForFlag(
        IServiceCollectionQuartzConfigurator configurator,
        JobTriggerFlags flag,
        string jobKey,
        TimeSpan specificTime)
    {
        const string triggerPrefix = "trigger";
        var flagName = flag.ToString().ToLower();
        configurator.AddTrigger(opt =>
        {
            opt
                .ForJob(jobKey)
                .WithIdentity($"{jobKey}.{flagName}.{triggerPrefix}");

            if (flag != JobTriggerFlags.OnStartup)
            {
                opt.StartAt(DateBuilder.DateOf(specificTime.Hours, specificTime.Minutes, 0));
            }

            switch (flag)
            {
                case JobTriggerFlags.OnStartup:
                    opt.StartNow();
                    break;
                case JobTriggerFlags.EveryMinute:
                    opt
                        .WithCalendarIntervalSchedule(builder => builder
                            .WithIntervalInMinutes(1));
                    break;
                case JobTriggerFlags.EveryHour:
                    opt
                        .WithCalendarIntervalSchedule(builder => builder
                            .WithIntervalInHours(1));
                    break;
                case JobTriggerFlags.Daily:
                    opt
                        .WithCalendarIntervalSchedule(builder => builder
                            .WithIntervalInDays(1));
                    break;
                case JobTriggerFlags.Weekly:
                    opt
                        .WithCalendarIntervalSchedule(builder => builder
                            .WithIntervalInWeeks(1));
                    break;
                case JobTriggerFlags.Monthly:
                    opt
                        .WithCalendarIntervalSchedule(builder => builder
                            .WithIntervalInMonths(1));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag), flag, null);
            }
        });
    }

    /// <summary>
    /// Проверяет входные параметры на null или пустые значения.
    /// </summary>
    private static void ValidateParameters(
        string jobKey,
        bool isWrapperJob,
        string? cronExpression = null,
        Func<IServiceProvider, CancellationToken, Task>? job = null)
    {
        if (string.IsNullOrWhiteSpace(jobKey))
        {
            throw new ArgumentNullException(nameof(jobKey), "Ключ задачи не может быть null или пустым.");
        }

        if (cronExpression != null && string.IsNullOrWhiteSpace(cronExpression))
        {
            throw new ArgumentNullException(nameof(cronExpression), "CRON-выражение не может быть null или пустым.");
        }

        if (isWrapperJob && job == null)
        {
            throw new ArgumentNullException(nameof(job), "Действие задачи не может быть null.");
        }
    }

    private static IServiceCollection ConfigureQuartz(
        this IServiceCollection serviceCollection,
        Action<IServiceCollectionQuartzConfigurator> configurationAction)
    {
        return serviceCollection
            .AddQuartz(configurationAction)
            .AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    }
}
