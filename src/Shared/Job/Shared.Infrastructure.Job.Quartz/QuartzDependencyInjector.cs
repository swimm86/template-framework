// ----------------------------------------------------------------------------------------------
// <copyright file="QuartzDependencyInjector.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Shared.Application.Core.DependencyInjection.Base;
using Shared.Application.Core.Job.Scheduler.Interfaces;

namespace Shared.Infrastructure.Job.Quartz;

/// <summary>
/// Регистрация DI-зависимостей слоя <c>Shared.Infrastructure.Job.Quartz</c>.
/// <para>
/// Регистрирует:
/// <list type="bullet">
/// <item><description>Quartz-планировщик через <see cref="ServiceCollectionExtensions.AddQuartz(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Action{IServiceCollectionQuartzConfigurator})"/>;</description></item>
/// <item><description><c>IJobFactory</c> с поддержкой Microsoft DI через <see cref="IServiceCollectionQuartzConfigurator.UseMicrosoftDependencyInjectionJobFactory"/> — без этого Quartz использует встроенный <c>SimpleJobFactory</c>, который не может инжектить зависимости в конструктор <see cref="QuartzScheduledJobAdapter"/>, и ни одна джоба фактически не выполняется;</description></item>
/// <item><description><see cref="IJobScheduler"/> в лице <see cref="QuartzJobScheduler"/>;</description></item>
/// <item><description><see cref="IHostedService"/> в лице <see cref="QuartzJobSchedulerBootstrapper"/> — регистрирует все задачи из <c>JobSchedulerOptions</c> и стартует/останавливает планировщик при старте/остановке хоста.</description></item>
/// </list>
/// </para>
/// <para>
/// <see cref="ServiceCollectionExtensions.AddQuartz(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Action{IServiceCollectionQuartzConfigurator})"/>
/// по умолчанию <b>не</b> регистрирует <see cref="IHostedService"/> для старта
/// планировщика — это нужно делать отдельно через
/// <c>AddQuartzHostedService</c>. Здесь это намеренно не делается, потому что
/// жизненный цикл <c>IScheduler</c> уже управляется
/// <see cref="QuartzJobSchedulerBootstrapper"/>: это позволяет логировать
/// количество зарегистрированных задач и зарегистрировать их <b>до</b> старта
/// планировщика (а не параллельно, как было бы при двух конкурирующих
/// <see cref="IHostedService"/>).
/// </para>
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
            .AddQuartz()
            .AddSingleton<IJobScheduler, QuartzJobScheduler>()
            .AddHostedService<QuartzJobSchedulerBootstrapper>();
    }
}
