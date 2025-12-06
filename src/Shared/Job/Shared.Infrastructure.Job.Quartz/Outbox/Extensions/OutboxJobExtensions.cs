// ----------------------------------------------------------------------------------------------
// <copyright file="OutboxJobExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Shared.Infrastructure.Job.Quartz.Outbox.Extensions;

/// <summary>
/// Расширения для регистрации Outbox Jobs.
/// </summary>
public static class OutboxJobExtensions
{
    /// <summary>
    /// Регистрирует задачу для обработки Outbox событий.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="cronExpression">CRON выражение для расписания. По умолчанию: каждую минуту.</param>
    /// <param name="batchSize">Размер батча событий. По умолчанию: 100.</param>
    /// <param name="lockDurationMinutes">Длительность блокировки в минутах. По умолчанию: 5.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddOutboxProcessorJob(
        this IServiceCollection services,
        string cronExpression = "0 * * * * ?", // Каждую минуту
        int batchSize = 100,
        int lockDurationMinutes = 5)
    {
        return services.ConfigureQuartz(q =>
        {
            var jobKey = new JobKey("OutboxProcessorJob");

            q.AddJob<OutboxProcessorJob>(opts =>
            {
                opts.WithIdentity(jobKey);
                opts.SetJobData(new JobDataMap
                {
                    { "BatchSize", batchSize },
                    { "LockDurationMinutes", lockDurationMinutes }
                });
            });

            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("OutboxProcessorJob-trigger")
                .WithCronSchedule(cronExpression));
        });
    }

    /// <summary>
    /// Регистрирует задачу для очистки Outbox событий.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="cronExpression">CRON выражение для расписания. По умолчанию: каждый час.</param>
    /// <param name="olderThanDays">Удалять события старше указанного количества дней. По умолчанию: 30.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddOutboxCleanupJob(
        this IServiceCollection services,
        string cronExpression = "0 0 * * * ?", // Каждый час
        int olderThanDays = 30)
    {
        return services.ConfigureQuartz(q =>
        {
            var jobKey = new JobKey("OutboxCleanupJob");

            q.AddJob<OutboxCleanupJob>(opts =>
            {
                opts.WithIdentity(jobKey);
                opts.SetJobData(new JobDataMap
                {
                    { "OlderThanDays", olderThanDays }
                });
            });

            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("OutboxCleanupJob-trigger")
                .WithCronSchedule(cronExpression));
        });
    }

    /// <summary>
    /// Регистрирует все Outbox Jobs с настройками по умолчанию.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="processorCronExpression">CRON выражение для процессора. По умолчанию: каждую минуту.</param>
    /// <param name="cleanupCronExpression">CRON выражение для очистки. По умолчанию: каждый час.</param>
    /// <param name="batchSize">Размер батча событий. По умолчанию: 100.</param>
    /// <param name="lockDurationMinutes">Длительность блокировки в минутах. По умолчанию: 5.</param>
    /// <param name="cleanupOlderThanDays">Удалять события старше указанного количества дней. По умолчанию: 30.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddOutboxJobs(
        this IServiceCollection services,
        string processorCronExpression = "0 * * * * ?",
        string cleanupCronExpression = "0 0 * * * ?",
        int batchSize = 100,
        int lockDurationMinutes = 5,
        int cleanupOlderThanDays = 30)
    {
        services
            .AddOutboxProcessorJob(processorCronExpression, batchSize, lockDurationMinutes)
            .AddOutboxCleanupJob(cleanupCronExpression, cleanupOlderThanDays);

        return services;
    }

    private static IServiceCollection ConfigureQuartz(
        this IServiceCollection services,
        Action<IServiceCollectionQuartzConfigurator> configurationAction)
    {
        return services
            .AddQuartz(configurationAction)
            .AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    }
}

