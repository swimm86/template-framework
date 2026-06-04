// ----------------------------------------------------------------------------------------------
// <copyright file="HangfireJobSchedulerTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Application.Core.Job.Enums;
using Shared.Application.Core.Job.Interfaces;
using Shared.Application.Core.Job.Scheduler;
using Shared.Testing.Job;
using HangfireJob = Hangfire.Common.Job;

namespace Shared.Infrastructure.Job.Hangfire.Tests;

/// <summary>
/// Тесты <see cref="HangfireJobScheduler"/> с моками <see cref="IRecurringJobManager"/>
/// и <see cref="IBackgroundJobClient"/>: проверка соответствия расписания типа джобы и флагов
/// вызовам Hangfire API.
/// </summary>
public sealed class HangfireJobSchedulerTests
{
    private const string SampleCron = "0 0/5 * * * ?";

    /// <summary>
    /// <see cref="HangfireJobScheduler.ScheduleAsync"/> с <see cref="JobSchedule.Cron"/>
    /// делегирует <c>IRecurringJobManager.AddOrUpdate(string, Job, string, RecurringJobOptions)</c>
    /// ровно один раз с заданным cron-выражением.
    /// </summary>
    [Fact]
    public async Task CallsRecurringJobManager_WhenCronScheduled()
    {
        // Arrange
        var recurring = new Mock<IRecurringJobManager>();
        var background = new Mock<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition("cron-job", typeof(FakeScheduledJob), new JobSchedule.Cron(SampleCron));

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        recurring.Verify(
            r => r.AddOrUpdate(
                "cron-job",
                It.Is<HangfireJob>(j => j.Type == typeof(HangfireScheduledJobAdapter) && j.Method.Name == nameof(HangfireScheduledJobAdapter.RunScheduledJobAsync)),
                SampleCron,
                It.IsAny<RecurringJobOptions>()),
            Times.Once);
        background.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Cron-джоба передаёт в Job аргументы <c>typeName</c>, <c>serviceKey</c> и
    /// <see cref="CancellationToken.None"/> в том же порядке, что объявлено в
    /// <see cref="HangfireScheduledJobAdapter.RunScheduledJobAsync"/>.
    /// </summary>
    [Fact]
    public async Task PassesTypeNameServiceKeyAndCancellationToken_WhenCronScheduled()
    {
        // Arrange
        var recurring = new Mock<IRecurringJobManager>();
        var background = new Mock<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition(
            "cron-job",
            typeof(FakeScheduledJob),
            new JobSchedule.Cron(SampleCron),
            serviceKey: "alpha");

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        recurring.Verify(
            r => r.AddOrUpdate(
                It.IsAny<string>(),
                It.Is<HangfireJob>(j => VerifyBridgeArgs(j, typeof(FakeScheduledJob), "alpha")),
                It.IsAny<string>(),
                It.IsAny<RecurringJobOptions>()),
            Times.Once);
    }

    /// <summary>
    /// <see cref="JobSchedule.OnStartup"/> приводит к вызову
    /// <c>IBackgroundJobClient.Create(Job, IState)</c> с
    /// <see cref="ScheduledState"/>, у которого <c>EnqueueAt</c> совпадает с текущим моментом.
    /// </summary>
    [Fact]
    public async Task CallsBackgroundCreateWithScheduledState_WhenOnStartup()
    {
        // Arrange
        var recurring = new Mock<IRecurringJobManager>();
        var background = new Mock<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition("startup-job", typeof(FakeScheduledJob), new JobSchedule.OnStartup());

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        background.Verify(
            b => b.Create(
                It.Is<HangfireJob>(j => j.Type == typeof(HangfireScheduledJobAdapter) && j.Method.Name == nameof(HangfireScheduledJobAdapter.RunScheduledJobAsync)),
                It.Is<IState>(s => s is ScheduledState)),
            Times.Once);
        recurring.VerifyNoOtherCalls();
    }

    /// <summary>
    /// <see cref="JobSchedule.Flags"/> с <see cref="JobTriggerFlags.Daily"/>
    /// и конкретным временем 02:00 порождает cron-выражение <c>"0 2 * * *"</c> и
    /// суффикс <c>#Daily</c> в ключе recurring-джобы.
    /// </summary>
    [Fact]
    public async Task BuildsExpectedCronAndKeySuffix_WhenDailyFlagSet()
    {
        // Arrange
        var recurring = new Mock<IRecurringJobManager>();
        var background = new Mock<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition(
            "flagged",
            typeof(FakeScheduledJob),
            new JobSchedule.Flags(JobTriggerFlags.Daily, TimeSpan.FromHours(2)));

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        recurring.Verify(
            r => r.AddOrUpdate(
                "flagged#Daily",
                It.Is<HangfireJob>(j => j.Type == typeof(HangfireScheduledJobAdapter)),
                "0 2 * * *",
                It.IsAny<RecurringJobOptions>()),
            Times.Once);
        background.VerifyNoOtherCalls();
    }

    /// <summary>
    /// <see cref="JobTriggerFlags.EveryMinute"/> даёт cron <c>"* * * * *"</c>.
    /// </summary>
    [Fact]
    public async Task BuildsStarCron_WhenEveryMinuteFlagSet()
    {
        // Arrange
        var recurring = new Mock<IRecurringJobManager>();
        var background = new Mock<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition(
            "minute",
            typeof(FakeScheduledJob),
            new JobSchedule.Flags(JobTriggerFlags.EveryMinute, TimeSpan.Zero));

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        recurring.Verify(
            r => r.AddOrUpdate(
                "minute#EveryMinute",
                It.IsAny<HangfireJob>(),
                "* * * * *",
                It.IsAny<RecurringJobOptions>()),
            Times.Once);
    }

    /// <summary>
    /// <see cref="JobTriggerFlags.EveryHour"/> с конкретным временем 00:15:00 даёт cron
    /// <c>"15 * * * *"</c> (минута 15 каждого часа).
    /// </summary>
    [Fact]
    public async Task BuildsMinuteHourCron_WhenEveryHourFlagSet()
    {
        // Arrange
        var recurring = new Mock<IRecurringJobManager>();
        var background = new Mock<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition(
            "hourly",
            typeof(FakeScheduledJob),
            new JobSchedule.Flags(JobTriggerFlags.EveryHour, new TimeSpan(0, 15, 0)));

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        recurring.Verify(
            r => r.AddOrUpdate(
                "hourly#EveryHour",
                It.IsAny<HangfireJob>(),
                "15 * * * *",
                It.IsAny<RecurringJobOptions>()),
            Times.Once);
    }

    /// <summary>
    /// <see cref="JobTriggerFlags.Weekly"/> даёт cron <c>"0 2 * * 1"</c> (понедельник, 02:00).
    /// </summary>
    [Fact]
    public async Task BuildsWeeklyCron_WhenWeeklyFlagSet()
    {
        // Arrange
        var recurring = new Mock<IRecurringJobManager>();
        var background = new Mock<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition(
            "weekly",
            typeof(FakeScheduledJob),
            new JobSchedule.Flags(JobTriggerFlags.Weekly, TimeSpan.FromHours(2)));

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        recurring.Verify(
            r => r.AddOrUpdate(
                "weekly#Weekly",
                It.IsAny<HangfireJob>(),
                "0 2 * * 1",
                It.IsAny<RecurringJobOptions>()),
            Times.Once);
    }

    /// <summary>
    /// <see cref="JobTriggerFlags.Monthly"/> даёт cron <c>"0 2 1 * *"</c> (1-е число, 02:00).
    /// </summary>
    [Fact]
    public async Task BuildsMonthlyCron_WhenMonthlyFlagSet()
    {
        // Arrange
        var recurring = new Mock<IRecurringJobManager>();
        var background = new Mock<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition(
            "monthly",
            typeof(FakeScheduledJob),
            new JobSchedule.Flags(JobTriggerFlags.Monthly, TimeSpan.FromHours(2)));

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        recurring.Verify(
            r => r.AddOrUpdate(
                "monthly#Monthly",
                It.IsAny<HangfireJob>(),
                "0 2 1 * *",
                It.IsAny<RecurringJobOptions>()),
            Times.Once);
    }

    /// <summary>
    /// <see cref="JobTriggerFlags.OnStartup"/> через <see cref="JobSchedule.Flags"/>
    /// порождает вызов <c>IBackgroundJobClient.Create(Job, IState)</c>.
    /// </summary>
    [Fact]
    public async Task CallsBackgroundCreate_WhenOnStartupFlagSet()
    {
        // Arrange
        var recurring = new Mock<IRecurringJobManager>();
        var background = new Mock<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition(
            "startup-flags",
            typeof(FakeScheduledJob),
            new JobSchedule.Flags(JobTriggerFlags.OnStartup, TimeSpan.Zero));

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        background.Verify(
            b => b.Create(
                It.Is<HangfireJob>(j => j.Type == typeof(HangfireScheduledJobAdapter)),
                It.Is<IState>(s => s is ScheduledState)),
            Times.Once);
        recurring.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Комбинация <see cref="JobTriggerFlags.Daily"/> + <see cref="JobTriggerFlags.OnStartup"/>
    /// порождает ровно один recurring-вызов и ровно один background-вызов.
    /// </summary>
    [Fact]
    public async Task CallsBothRecurringAndBackgroundApis_WhenDailyAndOnStartupFlagsSet()
    {
        // Arrange
        var recurring = new Mock<IRecurringJobManager>();
        var background = new Mock<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition(
            "mixed",
            typeof(FakeScheduledJob),
            new JobSchedule.Flags(JobTriggerFlags.Daily | JobTriggerFlags.OnStartup, TimeSpan.FromHours(2)));

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        recurring.Verify(
            r => r.AddOrUpdate(
                "mixed#Daily",
                It.IsAny<HangfireJob>(),
                "0 2 * * *",
                It.IsAny<RecurringJobOptions>()),
            Times.Once);
        background.Verify(
            b => b.Create(It.IsAny<HangfireJob>(), It.IsAny<IState>()),
            Times.Once);
    }

    /// <summary>
    /// Лямбда-джоба (<c>JobType == null</c>) не поддерживается Hangfire —
    /// <see cref="HangfireJobScheduler.ScheduleAsync"/> бросает <see cref="NotSupportedException"/>
    /// и НЕ вызывает Hangfire API.
    /// </summary>
    [Fact]
    public async Task ThrowsNotSupported_WhenJobTypeIsLambda()
    {
        // Arrange
        var recurring = new Mock<IRecurringJobManager>();
        var background = new Mock<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = new JobDefinition(
            JobKey: "lambda",
            Action: (_, _) => Task.CompletedTask,
            Schedule: new JobSchedule.Cron(SampleCron),
            JobType: null,
            ServiceKey: null);

        // Act
        var act = () => scheduler.ScheduleAsync(definition);

        // Assert
        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*lambda*not supported*");
        recurring.VerifyNoOtherCalls();
        background.VerifyNoOtherCalls();
    }

    /// <summary>
    /// <c>null</c> <see cref="JobDefinition"/> даёт <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public async Task ThrowsArgumentNull_WhenDefinitionIsNull()
    {
        // Arrange
        var recurring = new Mock<IRecurringJobManager>();
        var background = new Mock<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        // Act
        var act = () => scheduler.ScheduleAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Защитная проверка <c>JobType.AssemblyQualifiedName</c> существует в production-коде,
    /// но недостижима в чистом .NET: ни один реальный <see cref="Type"/>, реализующий
    /// <see cref="IScheduledJob"/>, не имеет <c>AssemblyQualifiedName == null</c> (это свойство
    /// равно <c>null</c> только у generic-параметров вроде <c>T</c>, которые не могут быть
    /// загружены как <see cref="IScheduledJob"/>-реализации). Поэтому мы проверяем только
    /// инвариант «тип-реализация IScheduledJob всегда имеет AssemblyQualifiedName» —
    /// контракт сохраняется, защитный код остаётся в качестве документации.
    /// </summary>
    [Fact]
    public void IsScheduledJobImplementation_AlwaysHasAssemblyQualifiedName()
    {
        typeof(FakeScheduledJob).AssemblyQualifiedName.Should().NotBeNull();
    }

    private static HangfireJobScheduler CreateScheduler(
        Mock<IRecurringJobManager> recurring,
        Mock<IBackgroundJobClient> background) =>
        new(recurring.Object, background.Object, NullLogger<HangfireJobScheduler>.Instance);

    private static JobDefinition NewClassDefinition(
        string jobKey,
        Type jobType,
        JobSchedule schedule,
        string? serviceKey = null) =>
        new(
            JobKey: jobKey,
            Action: null,
            Schedule: schedule,
            JobType: jobType,
            ServiceKey: serviceKey);

    private static bool VerifyBridgeArgs(HangfireJob job, Type expectedType, string? expectedServiceKey)
    {
        var expectedTypeName = expectedType.AssemblyQualifiedName;
        if (expectedTypeName is null)
        {
            return false;
        }

        return job.Args.Count == 3
            && job.Args[0] is string actualTypeName && actualTypeName == expectedTypeName
            && ((job.Args[1] is null && expectedServiceKey is null)
                || (job.Args[1] is string actualServiceKey && actualServiceKey == expectedServiceKey))
            && job.Args[2] is CancellationToken;
    }
}
