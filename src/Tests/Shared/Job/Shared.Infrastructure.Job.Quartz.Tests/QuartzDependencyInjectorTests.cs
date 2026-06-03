// ----------------------------------------------------------------------------------------------
// <copyright file="QuartzDependencyInjectorTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Shared.Application.Core.Job.Scheduler;
using Shared.Application.Core.Job.Scheduler.Interfaces;
using Shared.Testing.DependencyInjection;

namespace Shared.Infrastructure.Job.Quartz.Tests;

/// <summary>
/// Тесты DI-регистрации <see cref="QuartzDependencyInjector"/>.
/// </summary>
public sealed class QuartzDependencyInjectorTests
{
    /// <summary>
    /// <see cref="QuartzDependencyInjector"/> регистрирует
    /// <see cref="ISchedulerFactory"/>, <see cref="IJobScheduler"/> и
    /// <see cref="Microsoft.Extensions.Hosting.IHostedService"/> для bootstrapper-а.
    /// </summary>
    [Fact]
    public void Inject_RegistersAllRequiredServices()
    {
        // Arrange
        using var sp = ServiceProviderBuilder.Build(services =>
        {
            services.AddLogging();
            services.AddSingleton(new JobSchedulerOptions());
            new QuartzDependencyInjector(
                    LoggerFactory.Create(b => { }))
                .Inject(services);
        });

        // Act / Assert
        sp.GetRequiredService<ISchedulerFactory>().Should().NotBeNull();
        sp.GetRequiredService<IJobScheduler>().Should().BeOfType<QuartzJobScheduler>();
        sp.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .OfType<QuartzJobSchedulerBootstrapper>()
            .Should()
            .HaveCount(1);
    }
}
