// ----------------------------------------------------------------------------------------------
// <copyright file="QuartzScheduledJobAdapterTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;
using Shared.Application.Core.Job.Interfaces;
using Shared.Application.Core.Job.Pipeline;
using Shared.Application.Core.Job.Pipeline.Interfaces;
using Shared.Application.Core.Job.Scheduler;
using Shared.Testing.Doubles.Logging;

namespace Shared.Infrastructure.Job.Quartz.Tests;

/// <summary>
/// Тесты <see cref="QuartzScheduledJobAdapter"/>: проверяем, что адаптер
/// корректно читает <see cref="JobDataMap"/>, формирует
/// <see cref="ScheduledJobContext"/> и передаёт его в
/// <see cref="IScheduledJobExecutor.ExecuteAsync"/>.
/// </summary>
public sealed class QuartzScheduledJobAdapterTests
{
    /// <summary>
    /// Если в <see cref="JobDataMap"/> нет ни <c>JobType</c>, ни <c>JobAction</c>,
    /// адаптер логирует ошибку и не вызывает executor.
    /// </summary>
    [Fact]
    public async Task Execute_NoJobTypeNoAction_LogsErrorAndSkipsExecutor()
    {
        // Arrange
        var executor = new Mock<IScheduledJobExecutor>();
        var logger = new FakeLogger();
        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor.Object,
            new FakeLogger<QuartzScheduledJobAdapter>(logger));

        var context = NewExecutionContext("empty", new JobDataMap(), TestContext.Current.CancellationToken);

        // Act
        await adapter.Execute(context);

        // Assert
        executor.Verify(
            e => e.ExecuteAsync(It.IsAny<ScheduledJobContext>()),
            Times.Never,
            "executor не должен вызываться, если нечего выполнять");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Error && e.Message.Contains("empty"));
    }

    /// <summary>
    /// Классовая джоба: <see cref="JobDataMap"/> содержит <c>JobType</c>
    /// с корректным <c>AssemblyQualifiedName</c>. Адаптер передаёт в executor
    /// контекст с этим <c>JobType</c> и без <c>Action</c>.
    /// </summary>
    [Fact]
    public async Task Execute_ClassJob_BuildsContextWithJobTypeAndNoAction()
    {
        // Arrange
        var executor = new Mock<IScheduledJobExecutor>();
        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor.Object,
            NullLogger<QuartzScheduledJobAdapter>.Instance);

        var data = new JobDataMap
        {
            [QuartzScheduledJobAdapter.JobTypeKey] = typeof(FakeScheduledJob).AssemblyQualifiedName!,
        };
        var context = NewExecutionContext("classJob", data, TestContext.Current.CancellationToken);

        ScheduledJobContext? captured = null;
        executor
            .Setup(e => e.ExecuteAsync(It.IsAny<ScheduledJobContext>()))
            .Callback<ScheduledJobContext>(ctx => captured = ctx)
            .Returns(Task.CompletedTask);

        // Act
        await adapter.Execute(context);

        // Assert
        captured.Should().NotBeNull();
        captured!.JobKey.Should().Be("classJob");
        captured.JobType.Should().Be<FakeScheduledJob>();
        captured.Action.Should().BeNull();
        captured.ServiceKey.Should().BeNull();
    }

    /// <summary>
    /// Классовая джоба с <see cref="JobDefinition.ServiceKey"/>:
    /// адаптер пробрасывает <c>ServiceKey</c> в контекст.
    /// </summary>
    [Fact]
    public async Task Execute_ClassJobWithServiceKey_ForwardsServiceKeyToContext()
    {
        // Arrange
        var executor = new Mock<IScheduledJobExecutor>();
        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor.Object,
            NullLogger<QuartzScheduledJobAdapter>.Instance);

        var data = new JobDataMap
        {
            [QuartzScheduledJobAdapter.JobTypeKey] = typeof(FakeScheduledJob).AssemblyQualifiedName!,
            [QuartzScheduledJobAdapter.ServiceKeyKey] = "primary",
        };
        var context = NewExecutionContext("keyed", data, TestContext.Current.CancellationToken);

        ScheduledJobContext? captured = null;
        executor
            .Setup(e => e.ExecuteAsync(It.IsAny<ScheduledJobContext>()))
            .Callback<ScheduledJobContext>(ctx => captured = ctx)
            .Returns(Task.CompletedTask);

        // Act
        await adapter.Execute(context);

        // Assert
        captured.Should().NotBeNull();
        captured!.JobKey.Should().Be("keyed");
        captured.ServiceKey.Should().Be("primary");
        captured.JobType.Should().Be<FakeScheduledJob>();
    }

    /// <summary>
    /// Лямбда-джоба: <c>JobType</c> в <see cref="JobDataMap"/> отсутствует,
    /// но есть <see cref="JobDefinition.ActionDataKey"/>. Адаптер передаёт
    /// делегат в <see cref="ScheduledJobContext.Action"/>.
    /// </summary>
    [Fact]
    public async Task Execute_LambdaJob_ForwardsActionToContext()
    {
        // Arrange
        var executor = new Mock<IScheduledJobExecutor>();
        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor.Object,
            NullLogger<QuartzScheduledJobAdapter>.Instance);

        Func<IServiceProvider, CancellationToken, Task> action = (_, _) => Task.CompletedTask;
        var data = new JobDataMap
        {
            [JobDefinition.ActionDataKey] = action,
        };
        var context = NewExecutionContext("lambda", data, TestContext.Current.CancellationToken);

        ScheduledJobContext? captured = null;
        executor
            .Setup(e => e.ExecuteAsync(It.IsAny<ScheduledJobContext>()))
            .Callback<ScheduledJobContext>(ctx => captured = ctx)
            .Returns(Task.CompletedTask);

        // Act
        await adapter.Execute(context);

        // Assert
        captured.Should().NotBeNull();
        captured!.JobType.Should().BeNull();
        captured.Action.Should().BeSameAs(action);
    }

    /// <summary>
    /// <c>JobType</c> в <see cref="JobDataMap"/> не резолвится в <see cref="Type"/>
    /// (например, искажённое имя сборки), но при этом есть <c>JobAction</c>:
    /// адаптер использует лямбду, а не пытается упасть на null-JobType.
    /// </summary>
    [Fact]
    public async Task Execute_UnresolvableJobTypeWithAction_FallsBackToAction()
    {
        // Arrange
        var executor = new Mock<IScheduledJobExecutor>();
        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor.Object,
            NullLogger<QuartzScheduledJobAdapter>.Instance);

        Func<IServiceProvider, CancellationToken, Task> action = (_, _) => Task.CompletedTask;
        var data = new JobDataMap
        {
            // Имя, которое точно не резолвится в Type.
            [QuartzScheduledJobAdapter.JobTypeKey] = "Definitely.Not.A.Type, Nowhere",
            [JobDefinition.ActionDataKey] = action,
        };
        var context = NewExecutionContext("fallback", data, TestContext.Current.CancellationToken);

        ScheduledJobContext? captured = null;
        executor
            .Setup(e => e.ExecuteAsync(It.IsAny<ScheduledJobContext>()))
            .Callback<ScheduledJobContext>(ctx => captured = ctx)
            .Returns(Task.CompletedTask);

        // Act
        await adapter.Execute(context);

        // Assert
        captured.Should().NotBeNull();
        captured!.JobType.Should().BeNull("нерезолвящийся JobType → null");
        captured.Action.Should().BeSameAs(action);
    }

    /// <summary>
    /// <see cref="CancellationToken"/> из <see cref="IJobExecutionContext"/>
    /// пробрасывается в <see cref="ScheduledJobContext.CancellationToken"/>.
    /// </summary>
    [Fact]
    public async Task Execute_ForwardsCancellationTokenToContext()
    {
        // Arrange
        var executor = new Mock<IScheduledJobExecutor>();
        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor.Object,
            NullLogger<QuartzScheduledJobAdapter>.Instance);

        using var cts = new CancellationTokenSource();
        var data = new JobDataMap
        {
            [QuartzScheduledJobAdapter.JobTypeKey] = typeof(FakeScheduledJob).AssemblyQualifiedName!,
        };
        var context = NewExecutionContext("ct", data, cts.Token);

        ScheduledJobContext? captured = null;
        executor
            .Setup(e => e.ExecuteAsync(It.IsAny<ScheduledJobContext>()))
            .Callback<ScheduledJobContext>(ctx => captured = ctx)
            .Returns(Task.CompletedTask);

        // Act
        await adapter.Execute(context);

        // Assert
        captured.Should().NotBeNull();
        captured!.CancellationToken.Should().Be(cts.Token);
    }

    /// <summary>
    /// <see cref="IServiceProvider"/>, переданный в адаптер, пробрасывается
    /// в <see cref="ScheduledJobContext.ServiceProvider"/> без подмены.
    /// </summary>
    [Fact]
    public async Task Execute_ForwardsServiceProviderToContext()
    {
        // Arrange
        var executor = new Mock<IScheduledJobExecutor>();
        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor.Object,
            NullLogger<QuartzScheduledJobAdapter>.Instance);

        var data = new JobDataMap
        {
            [QuartzScheduledJobAdapter.JobTypeKey] = typeof(FakeScheduledJob).AssemblyQualifiedName!,
        };
        var context = NewExecutionContext("sp", data, TestContext.Current.CancellationToken);

        ScheduledJobContext? captured = null;
        executor
            .Setup(e => e.ExecuteAsync(It.IsAny<ScheduledJobContext>()))
            .Callback<ScheduledJobContext>(ctx => captured = ctx)
            .Returns(Task.CompletedTask);

        // Act
        await adapter.Execute(context);

        // Assert
        captured.Should().NotBeNull();
        captured!.ServiceProvider.Should().BeSameAs(sp);
    }

    /// <summary>
    /// <c>JobKey</c> из <see cref="IJobDetail.Key"/> адаптера
    /// пробрасывается в <see cref="ScheduledJobContext.JobKey"/>.
    /// </summary>
    [Fact]
    public async Task Execute_ForwardsJobKeyFromIJobDetail()
    {
        // Arrange
        var executor = new Mock<IScheduledJobExecutor>();
        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor.Object,
            NullLogger<QuartzScheduledJobAdapter>.Instance);

        var data = new JobDataMap
        {
            [QuartzScheduledJobAdapter.JobTypeKey] = typeof(FakeScheduledJob).AssemblyQualifiedName!,
        };
        var context = NewExecutionContext("billing-jobs-nightly", data, TestContext.Current.CancellationToken);

        ScheduledJobContext? captured = null;
        executor
            .Setup(e => e.ExecuteAsync(It.IsAny<ScheduledJobContext>()))
            .Callback<ScheduledJobContext>(ctx => captured = ctx)
            .Returns(Task.CompletedTask);

        // Act
        await adapter.Execute(context);

        // Assert
        captured.Should().NotBeNull();
        captured!.JobKey.Should().Be("billing-jobs-nightly");
    }

    /// <summary>
    /// Исключение из <see cref="IScheduledJobExecutor.ExecuteAsync"/> пробрасывается
    /// вызывающему коду (Quartz ожидает это поведение для собственного retry-механизма).
    /// </summary>
    [Fact]
    public async Task Execute_WhenExecutorThrows_PropagatesException()
    {
        // Arrange
        var executor = new Mock<IScheduledJobExecutor>();
        executor
            .Setup(e => e.ExecuteAsync(It.IsAny<ScheduledJobContext>()))
            .ThrowsAsync(new InvalidOperationException("executor boom"));

        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor.Object,
            NullLogger<QuartzScheduledJobAdapter>.Instance);

        var data = new JobDataMap
        {
            [QuartzScheduledJobAdapter.JobTypeKey] = typeof(FakeScheduledJob).AssemblyQualifiedName!,
        };
        var context = NewExecutionContext("boom", data, TestContext.Current.CancellationToken);

        // Act
        var act = () => adapter.Execute(context);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("executor boom");
    }

    /// <summary>
    /// Создаёт <see cref="IJobExecutionContext"/> на базе <see cref="Mock{T}"/>
    /// с заданным <see cref="JobKey"/>, <see cref="JobDataMap"/> и
    /// <see cref="CancellationToken"/>.
    /// </summary>
    private static IJobExecutionContext NewExecutionContext(
        string jobKey,
        JobDataMap data,
        CancellationToken ct = default)
    {
        var jobDetail = new Mock<IJobDetail>();
        jobDetail.SetupGet(d => d.Key).Returns(new JobKey(jobKey));
        jobDetail.SetupGet(d => d.JobDataMap).Returns(data);

        var ctx = new Mock<IJobExecutionContext>();
        ctx.SetupGet(c => c.JobDetail).Returns(jobDetail.Object);
        ctx.SetupGet(c => c.CancellationToken).Returns(ct);
        return ctx.Object;
    }

    /// <summary>
    /// Тестовая классовая джоба для <see cref="JobDataMap"/>.
    /// </summary>
    private sealed class FakeScheduledJob : IScheduledJob
    {
        public Task ExecuteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
