// ----------------------------------------------------------------------------------------------
// <copyright file="HangfireDependencyInjectorTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Job.Extensions;
using Shared.Application.Core.Job.Scheduler.Interfaces;
using Shared.Testing.DependencyInjection;

namespace Shared.Infrastructure.Job.Hangfire.Tests;

/// <summary>
/// Тесты DI-регистрации <see cref="HangfireDependencyInjector"/>: проверяем, что
/// <see cref="IJobScheduler"/>, <see cref="HangfireScheduledJobAdapter"/>, <see cref="IHostedService"/>,
/// <see cref="IRecurringJobManager"/>, <see cref="IBackgroundJobClient"/> корректно
/// резолвятся из <see cref="IServiceProvider"/>.
/// </summary>
public sealed class HangfireDependencyInjectorTests
{
    /// <summary>
    /// <see cref="HangfireDependencyInjector"/> регистрирует
    /// <see cref="IJobScheduler"/>, <see cref="HangfireScheduledJobAdapter"/>,
    /// <see cref="IHostedService"/>-bootstrapper, <see cref="IRecurringJobManager"/>
    /// и <see cref="IBackgroundJobClient"/>.
    /// </summary>
    [Fact]
    public void Inject_RegistersAllRequiredServices()
    {
        // Arrange
        using var sp = ServiceProviderBuilder.Build(services =>
        {
            services.AddLogging();
            services.AddJobs(_ => { });
            new HangfireDependencyInjector(LoggerFactory.Create(b => { }))
                .Inject(services);
        });

        // Act / Assert
        sp.GetRequiredService<IJobScheduler>().Should().BeOfType<HangfireJobScheduler>();
        sp.GetRequiredService<HangfireScheduledJobAdapter>().Should().NotBeNull();
        sp.GetServices<IHostedService>()
            .OfType<HangfireJobSchedulerBootstrapper>()
            .Should()
            .HaveCount(1);
        sp.GetRequiredService<IRecurringJobManager>().Should().NotBeNull();
        sp.GetRequiredService<IBackgroundJobClient>().Should().NotBeNull();
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
            services.AddJobs(_ => { });
            new HangfireDependencyInjector(LoggerFactory.Create(b => { }))
                .Inject(services);
        });

        // Act
        var first = sp.GetRequiredService<IJobScheduler>();
        var second = sp.GetRequiredService<IJobScheduler>();

        // Assert
        first.Should().BeSameAs(second);
    }

    /// <summary>
    /// <see cref="HangfireScheduledJobAdapter"/> зарегистрирован как transient: два вызова
    /// <c>GetRequiredService</c> возвращают разные экземпляры.
    /// </summary>
    [Fact]
    public void Inject_HangfireScheduledJobAdapter_IsTransient()
    {
        // Arrange
        using var sp = ServiceProviderBuilder.Build(services =>
        {
            services.AddLogging();
            services.AddJobs(_ => { });
            new HangfireDependencyInjector(LoggerFactory.Create(b => { }))
                .Inject(services);
        });

        // Act
        var first = sp.GetRequiredService<HangfireScheduledJobAdapter>();
        var second = sp.GetRequiredService<HangfireScheduledJobAdapter>();

        // Assert
        first.Should().NotBeSameAs(second);
    }

    /// <summary>
    /// Повторный вызов <see cref="HangfireDependencyInjector.Inject"/> на той же
    /// коллекции сервисов не должен падать (всё регистрируется идемпотентно по
    /// API Hangfire).
    /// </summary>
    [Fact]
    public void Inject_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJobs(_ => { });
        var injector = new HangfireDependencyInjector(LoggerFactory.Create(b => { }));

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
        services.AddJobs(_ => { });
        services.AddSingleton("user-marker");

        // Act
        new HangfireDependencyInjector(LoggerFactory.Create(b => { }))
            .Inject(services);
        using var sp = services.BuildServiceProvider();

        // Assert
        sp.GetRequiredService<string>().Should().Be("user-marker");
    }
}
