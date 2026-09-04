// ----------------------------------------------------------------------------------------------
// <copyright file="QuartzScheduledJobAdapterTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Quartz;
using Shared.Application.Core.Job.Pipeline;
using Shared.Application.Core.Job.Pipeline.Interfaces;
using Shared.Testing.Doubles.Job;
using Shared.Testing.Doubles.Logging;
using Shared.Testing.Job;

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
    /// Если в <see cref="JobDataMap"/> нет ни <see cref="Constants.JobTypeKey"/>,
    /// ни <see cref="Constants.ActionDataKey"/>, адаптер логирует ошибку
    /// и не вызывает executor.
    /// </summary>
    [Fact]
    public async Task Execute_NoJobTypeNoAction_LogsErrorAndSkipsExecutor()
    {
        // Arrange
        var executor = new FakeScheduledJobExecutor();
        var logger = new FakeLogger();
        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor,
            new FakeLogger<QuartzScheduledJobAdapter>(logger));

        var context = NewExecutionContext("empty", new JobDataMap(), TestContext.Current.CancellationToken);

        // Act
        await adapter.Execute(context);

        // Assert
        executor.ExecuteAsyncCallCount.Should().Be(0,
            "executor не должен вызываться, если нечего выполнять");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Error && e.Message.Contains("empty"));
    }

    /// <summary>
    /// Классовая джоба: <see cref="JobDataMap"/> содержит
    /// <see cref="Constants.JobTypeKey"/> с корректным <c>AssemblyQualifiedName</c>.
    /// Адаптер передаёт в executor контекст с этим <c>JobType</c> и без <c>Action</c>.
    /// </summary>
    [Fact]
    public async Task Execute_ClassJob_BuildsContextWithJobTypeAndNoAction()
    {
        // Arrange
        var executor = new FakeScheduledJobExecutor();
        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor,
            NullLogger<QuartzScheduledJobAdapter>.Instance);

        var data = new JobDataMap
        {
            [Constants.JobTypeKey] = typeof(FakeScheduledJob).AssemblyQualifiedName!,
        };
        var context = NewExecutionContext("classJob", data, TestContext.Current.CancellationToken);

        // Act
        await adapter.Execute(context);

        // Assert
        executor.LastContext.Should().NotBeNull();
        executor.LastContext!.JobKey.Should().Be("classJob");
        executor.LastContext.JobType.Should().Be<FakeScheduledJob>();
        executor.LastContext.Action.Should().BeNull();
        executor.LastContext.ServiceKey.Should().BeNull();
    }

    /// <summary>
    /// Классовая джоба с <c>JobDefinition.ServiceKey</c>:
    /// адаптер пробрасывает <c>ServiceKey</c> в контекст.
    /// </summary>
    [Fact]
    public async Task Execute_ClassJobWithServiceKey_ForwardsServiceKeyToContext()
    {
        // Arrange
        var executor = new FakeScheduledJobExecutor();
        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor,
            NullLogger<QuartzScheduledJobAdapter>.Instance);

        var data = new JobDataMap
        {
            [Constants.JobTypeKey] = typeof(FakeScheduledJob).AssemblyQualifiedName!,
            [Constants.ServiceKeyKey] = "primary",
        };
        var context = NewExecutionContext("keyed", data, TestContext.Current.CancellationToken);

        // Act
        await adapter.Execute(context);

        // Assert
        executor.LastContext.Should().NotBeNull();
        executor.LastContext!.JobKey.Should().Be("keyed");
        executor.LastContext.ServiceKey.Should().Be("primary");
        executor.LastContext.JobType.Should().Be<FakeScheduledJob>();
    }

    /// <summary>
    /// Лямбда-джоба: <c>JobType</c> в <see cref="JobDataMap"/> отсутствует,
    /// но есть <see cref="Constants.ActionDataKey"/>. Адаптер передаёт
    /// делегат в <see cref="ScheduledJobContext.Action"/>.
    /// </summary>
    [Fact]
    public async Task Execute_LambdaJob_ForwardsActionToContext()
    {
        // Arrange
        var executor = new FakeScheduledJobExecutor();
        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor,
            NullLogger<QuartzScheduledJobAdapter>.Instance);

        Func<IServiceProvider, CancellationToken, Task> action = (_, _) => Task.CompletedTask;
        var data = new JobDataMap
        {
            [Constants.ActionDataKey] = action,
        };
        var context = NewExecutionContext("lambda", data, TestContext.Current.CancellationToken);

        // Act
        await adapter.Execute(context);

        // Assert
        executor.LastContext.Should().NotBeNull();
        executor.LastContext!.JobType.Should().BeNull();
        executor.LastContext.Action.Should().BeSameAs(action);
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
        var executor = new FakeScheduledJobExecutor();
        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor,
            NullLogger<QuartzScheduledJobAdapter>.Instance);

        Func<IServiceProvider, CancellationToken, Task> action = (_, _) => Task.CompletedTask;
        var data = new JobDataMap
        {
            // Имя, которое точно не резолвится в Type.
            [Constants.JobTypeKey] = "Definitely.Not.A.Type, Nowhere",
            [Constants.ActionDataKey] = action,
        };
        var context = NewExecutionContext("fallback", data, TestContext.Current.CancellationToken);

        // Act
        await adapter.Execute(context);

        // Assert
        executor.LastContext.Should().NotBeNull();
        executor.LastContext!.JobType.Should().BeNull("нерезолвящийся JobType → null");
        executor.LastContext.Action.Should().BeSameAs(action);
    }

    /// <summary>
    /// <see cref="CancellationToken"/> из <see cref="IJobExecutionContext"/>
    /// пробрасывается в <see cref="ScheduledJobContext.CancellationToken"/>.
    /// </summary>
    [Fact]
    public async Task Execute_ForwardsCancellationTokenToContext()
    {
        // Arrange
        var executor = new FakeScheduledJobExecutor();
        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor,
            NullLogger<QuartzScheduledJobAdapter>.Instance);

        using var cts = new CancellationTokenSource();
        var data = new JobDataMap
        {
            [Constants.JobTypeKey] = typeof(FakeScheduledJob).AssemblyQualifiedName!,
        };
        var context = NewExecutionContext("ct", data, cts.Token);

        // Act
        await adapter.Execute(context);

        // Assert
        executor.LastContext.Should().NotBeNull();
        executor.LastContext!.CancellationToken.Should().Be(cts.Token);
    }

    /// <summary>
    /// <see cref="IServiceProvider"/>, переданный в адаптер, пробрасывается
    /// в <see cref="ScheduledJobContext.ServiceProvider"/> без подмены.
    /// </summary>
    [Fact]
    public async Task Execute_ForwardsServiceProviderToContext()
    {
        // Arrange
        var executor = new FakeScheduledJobExecutor();
        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor,
            NullLogger<QuartzScheduledJobAdapter>.Instance);

        var data = new JobDataMap
        {
            [Constants.JobTypeKey] = typeof(FakeScheduledJob).AssemblyQualifiedName!,
        };
        var context = NewExecutionContext("sp", data, TestContext.Current.CancellationToken);

        // Act
        await adapter.Execute(context);

        // Assert
        executor.LastContext.Should().NotBeNull();
        executor.LastContext!.ServiceProvider.Should().BeSameAs(sp);
    }

    /// <summary>
    /// <c>JobKey</c> из <see cref="IJobDetail.Key"/> адаптера
    /// пробрасывается в <see cref="ScheduledJobContext.JobKey"/>.
    /// </summary>
    [Fact]
    public async Task Execute_ForwardsJobKeyFromIJobDetail()
    {
        // Arrange
        var executor = new FakeScheduledJobExecutor();
        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor,
            NullLogger<QuartzScheduledJobAdapter>.Instance);

        var data = new JobDataMap
        {
            [Constants.JobTypeKey] = typeof(FakeScheduledJob).AssemblyQualifiedName!,
        };
        var context = NewExecutionContext("billing-jobs-nightly", data, TestContext.Current.CancellationToken);

        // Act
        await adapter.Execute(context);

        // Assert
        executor.LastContext.Should().NotBeNull();
        executor.LastContext!.JobKey.Should().Be("billing-jobs-nightly");
    }

    /// <summary>
    /// <see cref="RetryOptions"/> из <see cref="Constants.RetryOptionsKey"/>
    /// в <see cref="JobDataMap"/> пробрасывается в <see cref="ScheduledJobContext.RetryOptions"/>.
    /// </summary>
    [Fact]
    public async Task Execute_WithRetryOptions_ForwardsToContext()
    {
        // Arrange
        var executor = new FakeScheduledJobExecutor();
        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor,
            NullLogger<QuartzScheduledJobAdapter>.Instance);

        var retryOptions = new RetryOptions
        {
            MaxAttempts = 5,
            Delay = TimeSpan.FromMinutes(1),
        };
        var data = new JobDataMap
        {
            [Constants.JobTypeKey] = typeof(FakeScheduledJob).AssemblyQualifiedName!,
            [Constants.RetryOptionsKey] = retryOptions,
        };
        var context = NewExecutionContext("with-retry", data, TestContext.Current.CancellationToken);

        // Act
        await adapter.Execute(context);

        // Assert
        executor.LastContext.Should().NotBeNull();
        executor.LastContext!.RetryOptions.Should().BeSameAs(retryOptions);
    }

    /// <summary>
    /// Если <see cref="Constants.RetryOptionsKey"/> отсутствует в <see cref="JobDataMap"/>,
    /// <see cref="ScheduledJobContext.RetryOptions"/> остаётся <c>null</c> —
    /// <c>RetryMiddleware</c> интерпретирует это как «без retry».
    /// </summary>
    [Fact]
    public async Task Execute_WithoutRetryOptions_LeavesContextRetryOptionsNull()
    {
        // Arrange
        var executor = new FakeScheduledJobExecutor();
        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor,
            NullLogger<QuartzScheduledJobAdapter>.Instance);

        var data = new JobDataMap
        {
            [Constants.JobTypeKey] = typeof(FakeScheduledJob).AssemblyQualifiedName!,
        };
        var context = NewExecutionContext("no-retry", data, TestContext.Current.CancellationToken);

        // Act
        await adapter.Execute(context);

        // Assert
        executor.LastContext.Should().NotBeNull();
        executor.LastContext!.RetryOptions.Should().BeNull();
    }

    /// <summary>
    /// Исключение из <see cref="IScheduledJobExecutor.ExecuteAsync"/> пробрасывается
    /// вызывающему коду (Quartz ожидает это поведение для собственного retry-механизма).
    /// </summary>
    [Fact]
    public async Task Execute_WhenExecutorThrows_PropagatesException()
    {
        // Arrange
        var executor = new FakeScheduledJobExecutor
        {
            ExceptionToThrowOnExecuteAsync = new InvalidOperationException("executor boom"),
        };
        var sp = new ServiceCollection().BuildServiceProvider();
        var adapter = new QuartzScheduledJobAdapter(
            sp,
            executor,
            NullLogger<QuartzScheduledJobAdapter>.Instance);

        var data = new JobDataMap
        {
            [Constants.JobTypeKey] = typeof(FakeScheduledJob).AssemblyQualifiedName!,
        };
        var context = NewExecutionContext("boom", data, TestContext.Current.CancellationToken);

        // Act
        var act = () => adapter.Execute(context);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("executor boom");
    }

    /// <summary>
    /// Создаёт <see cref="IJobExecutionContext"/> на базе NSubstitute-сабститутов
    /// с заданным <see cref="JobKey"/>, <see cref="JobDataMap"/> и
    /// <see cref="CancellationToken"/>.
    /// </summary>
    private static IJobExecutionContext NewExecutionContext(
        string jobKey,
        JobDataMap data,
        CancellationToken ct = default)
    {
        var jobDetail = Substitute.For<IJobDetail>();
        jobDetail.Key.Returns(new JobKey(jobKey));
        jobDetail.JobDataMap.Returns(data);

        var ctx = Substitute.For<IJobExecutionContext>();
        ctx.JobDetail.Returns(jobDetail);
        ctx.CancellationToken.Returns(ct);
        return ctx;
    }

}
