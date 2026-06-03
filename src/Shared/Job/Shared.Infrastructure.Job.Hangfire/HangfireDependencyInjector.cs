// ----------------------------------------------------------------------------------------------
// <copyright file="HangfireDependencyInjector.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection.Base;
using Shared.Application.Core.Job.Scheduler.Interfaces;

namespace Shared.Infrastructure.Job.Hangfire;

/// <summary>
/// Регистрация DI-зависимостей слоя <c>Shared.Infrastructure.Job.Hangfire</c>.
/// Регистрирует Hangfire-планировщик (in-memory для PoC), <see cref="IJobScheduler"/>,
/// <see cref="IHostedService"/>-bootstrapper.
/// </summary>
/// <inheritdoc cref="DependencyInjectorBase" path="/remarks"/>
/// <param name="loggerFactory"><inheritdoc cref="DependencyInjectorBase(ILoggerFactory)" path="/param[@name='loggerFactory']"/></param>
public class HangfireDependencyInjector(
    ILoggerFactory loggerFactory)
    : DependencyInjectorBase(loggerFactory)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddHangfire(config => config
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseInMemoryStorage())
            .AddHangfireServer()
            .AddTransient<HangfireScheduledJobAdapter>()
            .AddSingleton<IJobScheduler, HangfireJobScheduler>()
            .AddHostedService<HangfireJobSchedulerBootstrapper>();
    }
}
