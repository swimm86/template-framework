// ----------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Application.Core.Job.Pipeline;
using Shared.Application.Core.Job.Pipeline.Interfaces;
using Shared.Application.Core.Job.Pipeline.Middlewares;
using Shared.Application.Core.Job.Scheduler;

namespace Shared.Application.Core.Job.Extensions;

/// <summary>
/// Главная точка входа регистрации задач в DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует фоновые задачи, исполнитель (<see cref="IScheduledJobExecutor"/>) и стандартные посредники (<see cref="IScheduledJobMiddleware"/>).
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configure">Действие конфигурации построителя (<see cref="JobSchedulerBuilder"/>).</param>
    /// <returns>Коллекция сервисов.</returns>
    public static IServiceCollection AddJobs(
        this IServiceCollection services,
        Action<JobSchedulerBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new JobSchedulerBuilder();
        configure(builder);

        var options = builder.BuildOptions();
        services.AddSingleton(options);

        services.TryAddSingleton<IScheduledJobExecutor, ScheduledJobExecutor>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IScheduledJobMiddleware, CorrelationIdMiddleware>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IScheduledJobMiddleware, LoggingMiddleware>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IScheduledJobMiddleware, RetryMiddleware>());

        // Дефолтные настройки retry-middleware.
        services.TryAddSingleton<RetryOptions>();

        return services;
    }
}
