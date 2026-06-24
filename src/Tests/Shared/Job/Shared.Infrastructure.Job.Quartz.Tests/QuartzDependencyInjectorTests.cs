// ----------------------------------------------------------------------------------------------
// <copyright file="QuartzDependencyInjectorTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Spi;
using Shared.Application.Core.Job.Scheduler;
using Shared.Application.Core.Job.Scheduler.Interfaces;
using Shared.Testing.DependencyInjection;

namespace Shared.Infrastructure.Job.Quartz.Tests;

/// <summary>
/// Тесты DI-регистрации <see cref="QuartzDependencyInjector"/>.
/// <para>
/// Структура тестов полностью симметрична <c>HangfireDependencyInjectorTests</c>
/// (общий контракт: <c>Inject_RegistersAllRequiredServices</c>,
/// <c>Inject_IJobScheduler_IsSingleton</c>, <c>Inject_CalledTwice_DoesNotThrow</c>,
/// <c>Inject_PreservesExternalRegistrations</c> + специфичный для адаптера
/// <c>Inject_RegistersQuartzSpecificServices</c>) — иначе нельзя гарантировать
/// Zero-Touch Proof (смена Quartz ↔ Hangfire = 0 правок).
/// </para>
/// </summary>
public sealed class QuartzDependencyInjectorTests
{
    /// <summary>
    /// <see cref="QuartzDependencyInjector"/> регистрирует
    /// <see cref="IJobScheduler"/> в лице <see cref="QuartzJobScheduler"/> и
    /// <see cref="IHostedService"/> в лице <see cref="QuartzJobSchedulerBootstrapper"/>.
    /// </summary>
    [Fact]
    public void Inject_RegistersAllRequiredServices()
    {
        // Arrange
        using var sp = ServiceProviderBuilder.Build(services =>
        {
            services.AddLogging();
            services.AddSingleton(new JobSchedulerOptions());
            new QuartzDependencyInjector(LoggerFactory.Create(_ => { }))
                .Inject(services);
        });

        // Act / Assert
        sp.GetRequiredService<IJobScheduler>().Should().BeOfType<QuartzJobScheduler>();
        sp.GetServices<IHostedService>()
            .OfType<QuartzJobSchedulerBootstrapper>()
            .Should()
            .HaveCount(1);
    }

    /// <summary>
    /// <see cref="IJobScheduler"/> зарегистрирован как singleton: два вызова
    /// <c>GetRequiredService</c> возвращают один и тот же экземпляр.
    /// </summary>
    [Fact]
    public void Inject_IJobScheduler_IsSingleton()
    {
        // Arrange
        using var sp = ServiceProviderBuilder.Build(services =>
        {
            services.AddLogging();
            services.AddSingleton(new JobSchedulerOptions());
            new QuartzDependencyInjector(LoggerFactory.Create(_ => { }))
                .Inject(services);
        });

        // Act
        var first = sp.GetRequiredService<IJobScheduler>();
        var second = sp.GetRequiredService<IJobScheduler>();

        // Assert
        first.Should().BeSameAs(second);
    }

    /// <summary>
    /// Повторный вызов <c><see cref="Shared.Application.Core.DependencyInjection.Base.DependencyInjectorBase.Inject(IServiceCollection)"/></c> на той же
    /// коллекции сервисов не должен падать (всё регистрируется идемпотентно
    /// по API Quartz).
    /// </summary>
    [Fact]
    public void Inject_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new JobSchedulerOptions());
        var injector = new QuartzDependencyInjector(LoggerFactory.Create(_ => { }));

        // Act
        var act = () =>
        {
            injector.Inject(services);
            injector.Inject(services);
        };

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Injector пробрасывает <see cref="IServiceCollection"/> и не подменяет его
    /// на новый — добавленные пользователем сервисы остаются доступны.
    /// </summary>
    [Fact]
    public void Inject_PreservesExternalRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton("user-marker");

        // Act
        new QuartzDependencyInjector(LoggerFactory.Create(_ => { }))
            .Inject(services);
        using var sp = services.BuildServiceProvider();

        // Assert
        sp.GetRequiredService<string>().Should().Be("user-marker");
    }

    /// <summary>
    /// Специфичные для Quartz регистрации, не имеющие прямого аналога в Hangfire:
    /// <see cref="ISchedulerFactory"/> и <see cref="IJobFactory"/> с поддержкой
    /// Microsoft DI (<c>UseMicrosoftDependencyInjectionJobFactory</c>).
    /// Без <see cref="IJobFactory"/> Quartz не может инстанцировать
    /// <see cref="QuartzScheduledJobAdapter"/> через свой дефолтный
    /// <c>SimpleJobFactory</c> (у адаптера нет конструктора без параметров),
    /// и ни одна джоба фактически не выполняется (регрессия 2026-06-04).
    /// </summary>
    [Fact]
    public void Inject_RegistersQuartzSpecificServices()
    {
        // Arrange
        using var sp = ServiceProviderBuilder.Build(services =>
        {
            services.AddLogging();
            services.AddSingleton(new JobSchedulerOptions());
            new QuartzDependencyInjector(LoggerFactory.Create(_ => { }))
                .Inject(services);
        });

        // Act / Assert
        sp.GetRequiredService<ISchedulerFactory>().Should().NotBeNull();
        sp.GetService<IJobFactory>().Should().NotBeNull(
            "без IJobFactory с DI Quartz не сможет инстанцировать QuartzScheduledJobAdapter — " +
            "джобы не будут выполняться (регрессия 2026-06-04)");
    }
}
