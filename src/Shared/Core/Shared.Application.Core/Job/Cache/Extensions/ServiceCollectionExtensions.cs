// ----------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Cache;
using Shared.Application.Core.Cache.Interfaces;
using Shared.Application.Core.Job.Enums;
using Shared.Application.Core.Job.Extensions;
using Shared.Application.Core.Job.Scheduler;

namespace Shared.Application.Core.Job.Cache.Extensions;

/// <summary>
/// Методы регистрации фоновых задач для периодического обновления кэша:
/// обновляют <see cref="ICacheService{TData}"/> по расписанию через делегат,
/// получающий зависимости из DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует кэш-сервис и фоновую задачу для периодического обновления кэша по CRON-выражению.
    /// </summary>
    /// <typeparam name="TData">Тип данных кэша.</typeparam>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="cacheKey">Ключ кэша, используемый также как ключ фоновой задачи.</param>
    /// <param name="cronExpression">CRON-выражение.</param>
    /// <param name="getOrCreateFunc">Функция получения/обновления данных.</param>
    /// <returns>Коллекция сервисов.</returns>
    public static IServiceCollection AddCronCacheJob<TData>(
        this IServiceCollection services,
        string cacheKey,
        string cronExpression,
        Func<IServiceProvider, Task<TData>> getOrCreateFunc)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(getOrCreateFunc);

        services.RegisterCacheService(cacheKey, getOrCreateFunc);

        return services.AddJobs(opts => opts.AddJob(
            jobKey: cacheKey,
            schedule: new JobSchedule.Cron(cronExpression),
            action: (sp, _) => sp
                .GetRequiredKeyedService<ICacheService<TData>>(cacheKey)
                .UpdateCacheAsync()));
    }

    /// <typeparam name="TJob">Тип сервисной задачи кэширования.</typeparam>
    /// <inheritdoc cref="AddCronCacheJob{TData}"/>
    /// <typeparam name="TData"/>
    public static IServiceCollection AddCronCacheJob<TJob, TData>(
        this IServiceCollection services,
        string cacheKey,
        string cronExpression)
        where TJob : CacheUpdateJob<TData>
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddJobs(opts =>
                opts.AddJob<TJob>(new JobSchedule.Cron(cronExpression)))
            .RegisterCacheService<TJob, TData>(cacheKey);
    }

    /// <summary>
    /// Регистрирует кэш-сервис и фоновую задачу для периодического обновления кэша по флагам расписания.
    /// </summary>
    /// <param name="specificTime">Конкретное время выполнения.</param>
    /// <param name="cacheKey">Ключ кэша.</param>
    /// <param name="flags">Флаги расписания.</param>
    /// <inheritdoc cref="AddCronCacheJob{TData}"/>
    /// <param name="services"/><param name="getOrCreateFunc"/>
    public static IServiceCollection AddFlagsCacheJob<TData>(
        this IServiceCollection services,
        string cacheKey,
        JobTriggerFlags flags,
        TimeSpan specificTime,
        Func<IServiceProvider, Task<TData>> getOrCreateFunc)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(getOrCreateFunc);

        return services
            .RegisterCacheService(cacheKey, getOrCreateFunc)
            .AddJobs(opts => opts.AddJob(
            jobKey: cacheKey,
            schedule: new JobSchedule.Flags(flags, specificTime),
            action: (sp, _) => sp
                .GetRequiredKeyedService<ICacheService<TData>>(cacheKey)
                .UpdateCacheAsync()));
    }

    /// <typeparam name="TJob"><inheritdoc cref="AddCronCacheJob{TJob, TData}" path="/typeparam[@name='TJob']"/></typeparam>
    /// <inheritdoc cref="AddFlagsCacheJob{TData}"/>
    /// <typeparam name="TData"/>
    public static IServiceCollection AddFlagsCacheJob<TJob, TData>(
        this IServiceCollection services,
        string cacheKey,
        JobTriggerFlags flags,
        TimeSpan specificTime,
        Func<IServiceProvider, Task<TData>> getOrCreateFunc)
        where TJob : CacheUpdateJob<TData>
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(getOrCreateFunc);

        services.RegisterCacheService(cacheKey, getOrCreateFunc);

        return services
            .AddJobs(opts =>
                opts.AddJob<TJob>(new JobSchedule.Flags(flags, specificTime)))
            .RegisterCacheService<TJob, TData>(cacheKey);
    }

    private static IServiceCollection RegisterCacheService<TJob, TData>(
        this IServiceCollection services,
        string cacheKey)
        where TJob : CacheUpdateJob<TData>
    {
        return services
            .RegisterCacheService<TData>(
                cacheKey,
                serviceProvider => serviceProvider.GetRequiredService<TJob>().GetCacheDataAsync());
    }
}
