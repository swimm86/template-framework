// ----------------------------------------------------------------------------------------------
// <copyright file="DbSeederExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Job.Scheduler;

namespace Shared.Application.Core.Job.Extensions;

/// <summary>
/// Регистрация <see cref="DbSeederJob"/> в планировщике на запуск при старте.
/// </summary>
public static class DbSeederExtensions
{
    /// <summary>
    /// Регистрирует <see cref="DbSeederJob"/> как <see cref="Interfaces.IScheduledJob"/>,
    /// запускаемую один раз при старте приложения.
    /// Не зависит от Quartz.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <returns>Коллекция сервисов.</returns>
    public static IServiceCollection RegisterDbSeederJob(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddScoped<DbSeederJob>()
            .AddJobs(opts =>
                opts.AddJob<DbSeederJob>(new JobSchedule.OnStartup()));
    }
}
