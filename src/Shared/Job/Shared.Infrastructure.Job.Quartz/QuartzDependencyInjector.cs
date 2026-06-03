// ----------------------------------------------------------------------------------------------
// <copyright file="QuartzDependencyInjector.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Shared.Application.Core.DependencyInjection.Base;
using Shared.Application.Core.Job.Scheduler.Interfaces;

namespace Shared.Infrastructure.Job.Quartz;

/// <summary>
/// Регистрация DI-зависимостей слоя <c>Shared.Infrastructure.Job.Quartz</c>.
/// Регистрирует Quartz-планировщик, <see cref="IJobScheduler"/>, <see cref="IHostedService"/>
/// bootstrapper и адаптер для выполнения задач.
/// </summary>
/// <inheritdoc cref="DependencyInjectorBase" path="/remarks"/>
/// <param name="loggerFactory"><inheritdoc cref="DependencyInjectorBase(ILoggerFactory)" path="/param[@name='loggerFactory']"/></param>
public class QuartzDependencyInjector(
    ILoggerFactory loggerFactory)
    : DependencyInjectorBase(loggerFactory)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<ISchedulerFactory, StdSchedulerFactory>()
            .AddSingleton<IJobScheduler, QuartzJobScheduler>()
            .AddHostedService<QuartzJobSchedulerBootstrapper>();
    }
}
