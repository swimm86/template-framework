// ----------------------------------------------------------------------------------------------
// <copyright file="AddJobsExtensionTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Job.Extensions;
using Shared.Application.Core.Job.Pipeline.Interfaces;
using Shared.Application.Core.Job.Pipeline.Middlewares;
using Shared.Application.Core.Job.Scheduler;

namespace Shared.Application.Core.Tests;

/// <summary>
/// Тесты <c>AddJobs</c>: регистрирует все middleware, executor и options.
/// </summary>
public sealed class AddJobsExtensionTests
{
    /// <summary>
    /// <see cref="AddJobs"/> регистрирует JobSchedulerOptions с правильным списком джоб.
    /// </summary>
    [Fact]
    public void AddJobs_RegistersJobSchedulerOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddJobs(opts => opts.AddCron("job1", "0 0 * * * ?", (_, _) => Task.CompletedTask));

        // Act
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<JobSchedulerOptions>();

        // Assert
        options.Definitions.Should().HaveCount(1);
        options.Definitions[0].JobKey.Should().Be("job1");
    }

    /// <summary>
    /// <see cref="AddJobs"/> регистрирует <see cref="IScheduledJobExecutor"/> как singleton.
    /// </summary>
    [Fact]
    public void AddJobs_RegistersExecutorAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJobs(opts => opts.AddCron("job1", "0 0 * * * ?", (_, _) => Task.CompletedTask));

        // Act
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetRequiredService<IScheduledJobExecutor>().Should().NotBeNull();
    }

    /// <summary>
    /// <see cref="AddJobs"/> регистрирует три дефолтных middleware.
    /// </summary>
    [Fact]
    public void AddJobs_RegistersThreeDefaultMiddlewares()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJobs(opts => opts.AddCron("job1", "0 0 * * * ?", (_, _) => Task.CompletedTask));

        // Act
        var sp = services.BuildServiceProvider();
        var middlewares = sp.GetServices<IScheduledJobMiddleware>().ToArray();

        // Assert
        middlewares.OfType<LoggingMiddleware>().Should().HaveCount(1);
        middlewares.OfType<CorrelationIdMiddleware>().Should().HaveCount(1);
        middlewares.OfType<RetryMiddleware>().Should().HaveCount(1);
    }

    /// <summary>
    /// <see cref="AddJobs"/> можно вызывать несколько раз — JobSchedulerOptions перезаписывается
    /// на singleton (последний вызов побеждает), а middleware — TryAddEnumerable (накапливаются).
    /// </summary>
    [Fact]
    public void AddJobs_CalledTwice_OptionsReflectsLastCall()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddJobs(opts => opts.AddCron("job1", "0 0 * * * ?", (_, _) => Task.CompletedTask));
        services.AddJobs(opts => opts.AddCron("job2", "0 0 * * * ?", (_, _) => Task.CompletedTask));
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetRequiredService<JobSchedulerOptions>().Definitions.Should().HaveCount(1);
        sp.GetRequiredService<JobSchedulerOptions>().Definitions[0].JobKey.Should().Be("job2");
    }
}
