// ----------------------------------------------------------------------------------------------
// <copyright file="DbSeederExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Job.Pipeline;
using Shared.Application.Core.Job.Scheduler;

namespace Shared.Application.Core.Job.Extensions;

/// <summary>
/// Регистрация <see cref="DbSeederJob"/> в планировщике.
/// </summary>
public static class DbSeederExtensions
{
    /// <summary>
    /// Регистрирует <see cref="DbSeederJob"/> как <see cref="Interfaces.IScheduledJob"/>,
    /// запускаемую один раз при старте приложения.
    /// </summary>
    /// <remarks>
    /// При неудачной попытке предусмотрена политика повторов: раз в 5 минут в течении 100 раз.
    /// </remarks>
    /// <param name="services">Коллекция сервисов.</param>
    /// <returns>Коллекция сервисов.</returns>
    public static IServiceCollection RegisterDbSeederJob(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddScoped<DbSeederJob>()
            .AddJobs(opts =>
                opts.AddJob<DbSeederJob>(
                    new JobSchedule.OnStartup(),
                    new RetryOptions
                    {
                        Delay = new TimeSpan(0, 0, 5, 0),
                        MaxAttempts = 100,
                    }));
    }
}
