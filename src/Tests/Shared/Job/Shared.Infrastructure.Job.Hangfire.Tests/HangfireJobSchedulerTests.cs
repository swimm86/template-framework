// ----------------------------------------------------------------------------------------------
// <copyright file="HangfireJobSchedulerTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shared.Application.Core.Job.Enums;
using Shared.Application.Core.Job.Interfaces;
using Shared.Application.Core.Job.Pipeline;
using Shared.Application.Core.Job.Scheduler;
using Shared.Testing.Doubles.Logging;
using Shared.Testing.Job;
using HangfireJob = Hangfire.Common.Job;

namespace Shared.Infrastructure.Job.Hangfire.Tests;

/// <summary>
/// Тесты <see cref="HangfireJobScheduler"/> с substitutes <see cref="IRecurringJobManager"/>
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
        var recurring = Substitute.For<IRecurringJobManager>();
        var background = Substitute.For<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition("cron-job", typeof(FakeScheduledJob), new JobSchedule.Cron(SampleCron));

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        recurring.Received(1).AddOrUpdate(
            "cron-job",
            Arg.Is<HangfireJob>(j => j.Type == typeof(HangfireScheduledJobAdapter) && j.Method.Name == nameof(HangfireScheduledJobAdapter.RunScheduledJobAsync)),
            SampleCron,
            Arg.Any<RecurringJobOptions>());
        background.DidNotReceive().Create(Arg.Any<HangfireJob>(), Arg.Any<IState>());
        background.DidNotReceive().ChangeState(Arg.Any<string>(), Arg.Any<IState>(), Arg.Any<string>());
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
        var recurring = Substitute.For<IRecurringJobManager>();
        var background = Substitute.For<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition(
            "cron-job",
            typeof(FakeScheduledJob),
            new JobSchedule.Cron(SampleCron),
            serviceKey: "alpha");

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        recurring.Received(1).AddOrUpdate(
            Arg.Any<string>(),
            Arg.Is<HangfireJob>(j => VerifyBridgeArgs(j, typeof(FakeScheduledJob), "alpha")),
            Arg.Any<string>(),
            Arg.Any<RecurringJobOptions>());
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
        var recurring = Substitute.For<IRecurringJobManager>();
        var background = Substitute.For<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition("startup-job", typeof(FakeScheduledJob), new JobSchedule.OnStartup());

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        background.Received(1).Create(
            Arg.Is<HangfireJob>(j => j.Type == typeof(HangfireScheduledJobAdapter) && j.Method.Name == nameof(HangfireScheduledJobAdapter.RunScheduledJobAsync)),
            Arg.Is<IState>(s => s is ScheduledState));
        recurring.DidNotReceive().AddOrUpdate(Arg.Any<string>(), Arg.Any<HangfireJob>(), Arg.Any<string>(), Arg.Any<RecurringJobOptions>());
        recurring.DidNotReceive().Trigger(Arg.Any<string>());
        recurring.DidNotReceive().RemoveIfExists(Arg.Any<string>());
    }

    /// <summary>
    /// Cron-джоба передаёт <see cref="RetryOptions"/> в <c>HangfireJob.Args[2]</c>
    /// с тем же экземпляром, что указан в <see cref="JobDefinition.RetryOptions"/>.
    /// </summary>
    [Fact]
    public async Task PassesRetryOptions_WhenCronScheduled()
    {
        // Arrange
        var recurring = Substitute.For<IRecurringJobManager>();
        var background = Substitute.For<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var retryOptions = new RetryOptions
        {
            MaxAttempts = 4,
            Delay = TimeSpan.FromMinutes(1),
        };
        var definition = NewClassDefinition(
            "cron-with-retry",
            typeof(FakeScheduledJob),
            new JobSchedule.Cron(SampleCron),
            retryOptions: retryOptions);

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        recurring.Received(1).AddOrUpdate(
            Arg.Any<string>(),
            Arg.Is<HangfireJob>(j =>
                j.Args.Count == 4
                && j.Args[0] is string
                && ReferenceEquals(j.Args[2], retryOptions)),
            Arg.Any<string>(),
            Arg.Any<RecurringJobOptions>());
    }

    /// <summary>
    /// Cron-джоба без <see cref="RetryOptions"/>:
    /// <c>HangfireJob.Args[2]</c> = <c>null</c> (контракт: «нет retry»).
    /// </summary>
    [Fact]
    public async Task PassesNullRetryOptions_WhenRetryOptionsNotSpecified()
    {
        // Arrange
        var recurring = Substitute.For<IRecurringJobManager>();
        var background = Substitute.For<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition(
            "cron-no-retry",
            typeof(FakeScheduledJob),
            new JobSchedule.Cron(SampleCron));

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        recurring.Received(1).AddOrUpdate(
            Arg.Any<string>(),
            Arg.Is<HangfireJob>(j => j.Args.Count == 4 && j.Args[2] == null),
            Arg.Any<string>(),
            Arg.Any<RecurringJobOptions>());
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
        var recurring = Substitute.For<IRecurringJobManager>();
        var background = Substitute.For<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition(
            "flagged",
            typeof(FakeScheduledJob),
            new JobSchedule.Flags(JobTriggerFlags.Daily, TimeSpan.FromHours(2)));

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        recurring.Received(1).AddOrUpdate(
            "flagged#Daily",
            Arg.Is<HangfireJob>(j => j.Type == typeof(HangfireScheduledJobAdapter)),
            "0 2 * * *",
            Arg.Any<RecurringJobOptions>());
        background.DidNotReceive().Create(Arg.Any<HangfireJob>(), Arg.Any<IState>());
        background.DidNotReceive().ChangeState(Arg.Any<string>(), Arg.Any<IState>(), Arg.Any<string>());
    }

    /// <summary>
    /// <see cref="JobTriggerFlags.EveryMinute"/> даёт cron <c>"* * * * *"</c>.
    /// </summary>
    [Fact]
    public async Task BuildsStarCron_WhenEveryMinuteFlagSet()
    {
        // Arrange
        var recurring = Substitute.For<IRecurringJobManager>();
        var background = Substitute.For<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition(
            "minute",
            typeof(FakeScheduledJob),
            new JobSchedule.Flags(JobTriggerFlags.EveryMinute, TimeSpan.Zero));

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        recurring.Received(1).AddOrUpdate(
            "minute#EveryMinute",
            Arg.Any<HangfireJob>(),
            "* * * * *",
            Arg.Any<RecurringJobOptions>());
    }

    /// <summary>
    /// <see cref="JobTriggerFlags.EveryHour"/> с конкретным временем 00:15:00 даёт cron
    /// <c>"15 * * * *"</c> (минута 15 каждого часа).
    /// </summary>
    [Fact]
    public async Task BuildsMinuteHourCron_WhenEveryHourFlagSet()
    {
        // Arrange
        var recurring = Substitute.For<IRecurringJobManager>();
        var background = Substitute.For<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition(
            "hourly",
            typeof(FakeScheduledJob),
            new JobSchedule.Flags(JobTriggerFlags.EveryHour, new TimeSpan(0, 15, 0)));

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        recurring.Received(1).AddOrUpdate(
            "hourly#EveryHour",
            Arg.Any<HangfireJob>(),
            "15 * * * *",
            Arg.Any<RecurringJobOptions>());
    }

    /// <summary>
    /// <see cref="JobTriggerFlags.Weekly"/> даёт cron <c>"0 2 * * 1"</c> (понедельник, 02:00).
    /// </summary>
    [Fact]
    public async Task BuildsWeeklyCron_WhenWeeklyFlagSet()
    {
        // Arrange
        var recurring = Substitute.For<IRecurringJobManager>();
        var background = Substitute.For<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition(
            "weekly",
            typeof(FakeScheduledJob),
            new JobSchedule.Flags(JobTriggerFlags.Weekly, TimeSpan.FromHours(2)));

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        recurring.Received(1).AddOrUpdate(
            "weekly#Weekly",
            Arg.Any<HangfireJob>(),
            "0 2 * * 1",
            Arg.Any<RecurringJobOptions>());
    }

    /// <summary>
    /// <see cref="JobTriggerFlags.Monthly"/> даёт cron <c>"0 2 1 * *"</c> (1-е число, 02:00).
    /// </summary>
    [Fact]
    public async Task BuildsMonthlyCron_WhenMonthlyFlagSet()
    {
        // Arrange
        var recurring = Substitute.For<IRecurringJobManager>();
        var background = Substitute.For<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition(
            "monthly",
            typeof(FakeScheduledJob),
            new JobSchedule.Flags(JobTriggerFlags.Monthly, TimeSpan.FromHours(2)));

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        recurring.Received(1).AddOrUpdate(
            "monthly#Monthly",
            Arg.Any<HangfireJob>(),
            "0 2 1 * *",
            Arg.Any<RecurringJobOptions>());
    }

    /// <summary>
    /// <see cref="JobTriggerFlags.OnStartup"/> через <see cref="JobSchedule.Flags"/>
    /// порождает вызов <c>IBackgroundJobClient.Create(Job, IState)</c>.
    /// </summary>
    [Fact]
    public async Task CallsBackgroundCreate_WhenOnStartupFlagSet()
    {
        // Arrange
        var recurring = Substitute.For<IRecurringJobManager>();
        var background = Substitute.For<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition(
            "startup-flags",
            typeof(FakeScheduledJob),
            new JobSchedule.Flags(JobTriggerFlags.OnStartup, TimeSpan.Zero));

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        background.Received(1).Create(
            Arg.Is<HangfireJob>(j => j.Type == typeof(HangfireScheduledJobAdapter)),
            Arg.Is<IState>(s => s is ScheduledState));
        recurring.DidNotReceive().AddOrUpdate(Arg.Any<string>(), Arg.Any<HangfireJob>(), Arg.Any<string>(), Arg.Any<RecurringJobOptions>());
        recurring.DidNotReceive().Trigger(Arg.Any<string>());
        recurring.DidNotReceive().RemoveIfExists(Arg.Any<string>());
    }

    /// <summary>
    /// Комбинация <see cref="JobTriggerFlags.Daily"/> + <see cref="JobTriggerFlags.OnStartup"/>
    /// порождает ровно один recurring-вызов и ровно один background-вызов.
    /// </summary>
    [Fact]
    public async Task CallsBothRecurringAndBackgroundApis_WhenDailyAndOnStartupFlagsSet()
    {
        // Arrange
        var recurring = Substitute.For<IRecurringJobManager>();
        var background = Substitute.For<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = NewClassDefinition(
            "mixed",
            typeof(FakeScheduledJob),
            new JobSchedule.Flags(JobTriggerFlags.Daily | JobTriggerFlags.OnStartup, TimeSpan.FromHours(2)));

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        recurring.Received(1).AddOrUpdate(
            "mixed#Daily",
            Arg.Any<HangfireJob>(),
            "0 2 * * *",
            Arg.Any<RecurringJobOptions>());
        background.Received(1).Create(Arg.Any<HangfireJob>(), Arg.Any<IState>());
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
        var recurring = Substitute.For<IRecurringJobManager>();
        var background = Substitute.For<IBackgroundJobClient>();
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
        recurring.DidNotReceive().AddOrUpdate(Arg.Any<string>(), Arg.Any<HangfireJob>(), Arg.Any<string>(), Arg.Any<RecurringJobOptions>());
        recurring.DidNotReceive().Trigger(Arg.Any<string>());
        recurring.DidNotReceive().RemoveIfExists(Arg.Any<string>());
        background.DidNotReceive().Create(Arg.Any<HangfireJob>(), Arg.Any<IState>());
        background.DidNotReceive().ChangeState(Arg.Any<string>(), Arg.Any<IState>(), Arg.Any<string>());
    }

    /// <summary>
    /// <c>null</c> <see cref="JobDefinition"/> даёт <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public async Task ThrowsArgumentNull_WhenDefinitionIsNull()
    {
        // Arrange
        var recurring = Substitute.For<IRecurringJobManager>();
        var background = Substitute.For<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        // Act
        var act = () => scheduler.ScheduleAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Исключение из <see cref="IRecurringJobManager.AddOrUpdate"/> пробрасывается
    /// из <see cref="HangfireJobScheduler.ScheduleAsync"/> вызывающему коду —
    /// <c>bootstrapper.StartAsync</c> узнает об ошибке регистрации.
    /// <para>
    /// <b>Контракт:</b> если Hangfire storage недоступен (SQL/Redis down),
    /// <see cref="HangfireJobScheduler"/> НЕ проглатывает ошибку —
    /// <see cref="Exception"/> всплывает синхронно, потому что
    /// <see cref="IRecurringJobManager.AddOrUpdate"/> вызывается
    /// синхронно (без <c>await</c>) и любое исключение из API пробрасывается
    /// наверх через стек <c>ScheduleCron → ScheduleAsync → caller</c>.
    /// </para>
    /// <para>
    /// Это аналог поведения <c>QuartzJobScheduler.ScheduleAsync</c>, где
    /// <c>await scheduler.ScheduleJob(...)</c> тоже пробрасывает ошибки.
    /// </para>
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_PropagatesException_WhenRecurringApiThrows()
    {
        // Arrange
        var recurring = Substitute.For<IRecurringJobManager>();
        var background = Substitute.For<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var storageDown = new InvalidOperationException("Hangfire storage is down");
        recurring
            .When(r => r.AddOrUpdate(
                Arg.Any<string>(),
                Arg.Any<HangfireJob>(),
                Arg.Any<string>(),
                Arg.Any<RecurringJobOptions>()))
            .Do(_ => throw storageDown);

        var definition = NewClassDefinition(
            "broken-recurring",
            typeof(FakeScheduledJob),
            new JobSchedule.Cron(SampleCron));

        // Act + Assert — исключение пробрасывается в вызывающий код.
        var act = () => scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*storage is down*");
    }

    /// <summary>
    /// Исключение из <see cref="IBackgroundJobClient.Create"/> пробрасывается
    /// из <see cref="HangfireJobScheduler.ScheduleAsync"/> вызывающему коду.
    /// <para>
    /// Аналогично <see cref="ScheduleAsync_PropagatesException_WhenRecurringApiThrows"/>,
    /// но для <see cref="JobSchedule.OnStartup"/> через <see cref="IBackgroundJobClient"/>.
    /// </para>
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_PropagatesException_WhenBackgroundCreateThrows()
    {
        // Arrange
        var recurring = Substitute.For<IRecurringJobManager>();
        var background = Substitute.For<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var clientDown = new InvalidOperationException("BackgroundJobClient broken");
        background
            .Create(Arg.Any<HangfireJob>(), Arg.Any<IState>())
            .Returns(_ => throw clientDown);

        var definition = NewClassDefinition(
            "broken-startup",
            typeof(FakeScheduledJob),
            new JobSchedule.OnStartup());

        // Act + Assert
        var act = () => scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*BackgroundJobClient broken*");
    }

    /// <summary>
    /// Классовая джоба, чей <see cref="Type.AssemblyQualifiedName"/> равен <c>null</c>
    /// (например, открытый generic-параметр): <see cref="HangfireJobScheduler"/>
    /// бросает <see cref="InvalidOperationException"/> с упоминанием
    /// <c>AssemblyQualifiedName</c> — зеркалит
    /// <c>QuartzJobSchedulerTests.ScheduleAsync_JobTypeWithoutAssemblyQualifiedName_Throws</c>.
    /// </summary>
    [Fact]
    public async Task ThrowsInvalidOperation_WhenJobTypeHasNoAssemblyQualifiedName()
    {
        // Arrange
        var openGenericParam = typeof(GenericHolder<>).GetGenericArguments()[0];
        openGenericParam.AssemblyQualifiedName.Should().BeNull(
            "тест полагается на то, что у generic-параметра T нет AssemblyQualifiedName");

        var recurring = Substitute.For<IRecurringJobManager>();
        var background = Substitute.For<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = new JobDefinition(
            JobKey: "noAqn",
            Action: null,
            Schedule: new JobSchedule.Cron(SampleCron),
            JobType: openGenericParam);

        // Act
        var act = () => scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AssemblyQualifiedName*");
        recurring.DidNotReceive().AddOrUpdate(Arg.Any<string>(), Arg.Any<HangfireJob>(), Arg.Any<string>(), Arg.Any<RecurringJobOptions>());
        background.DidNotReceive().Create(Arg.Any<HangfireJob>(), Arg.Any<IState>());
    }

    /// <summary>
    /// Неизвестный подтип <see cref="JobSchedule"/> выбрасывает
    /// <see cref="ArgumentOutOfRangeException"/> — зеркалит
    /// <c>QuartzJobSchedulerTests.ScheduleAsync_UnknownJobSchedule_Throws</c>.
    /// </summary>
    [Fact]
    public async Task ThrowsArgumentOutOfRange_WhenJobScheduleIsUnknown()
    {
        // Arrange
        var recurring = Substitute.For<IRecurringJobManager>();
        var background = Substitute.For<IBackgroundJobClient>();
        var scheduler = CreateScheduler(recurring, background);

        var definition = new JobDefinition(
            JobKey: "unknownSchedule",
            Action: null,
            Schedule: new UnknownJobSchedule(),
            JobType: typeof(FakeScheduledJob));

        // Act
        var act = () => scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        recurring.DidNotReceive().AddOrUpdate(Arg.Any<string>(), Arg.Any<HangfireJob>(), Arg.Any<string>(), Arg.Any<RecurringJobOptions>());
        background.DidNotReceive().Create(Arg.Any<HangfireJob>(), Arg.Any<IState>());
    }

    /// <summary>
    /// <see cref="HangfireJobScheduler"/> логирует информационное сообщение с
    /// <c>JobKey</c> при регистрации cron-джобы — зеркалит
    /// <c>QuartzJobSchedulerTests.ScheduleAsync_LogsInformation</c>.
    /// </summary>
    [Fact]
    public async Task LogsInformation_WhenCronScheduled()
    {
        // Arrange
        var logger = new FakeLogger();
        var recurring = Substitute.For<IRecurringJobManager>();
        var background = Substitute.For<IBackgroundJobClient>();
        var scheduler = new HangfireJobScheduler(
            recurring,
            background,
            new FakeLogger<HangfireJobScheduler>(logger));

        var definition = NewClassDefinition(
            "logged",
            typeof(FakeScheduledJob),
            new JobSchedule.Cron(SampleCron));

        // Act
        await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Information);
        logger.Entries.Single().Message.Should().Contain("logged");
    }

    private static HangfireJobScheduler CreateScheduler(
        IRecurringJobManager recurring,
        IBackgroundJobClient background) =>
        new(recurring, background, NullLogger<HangfireJobScheduler>.Instance);

    private static JobDefinition NewClassDefinition(
        string jobKey,
        Type jobType,
        JobSchedule schedule,
        string? serviceKey = null,
        RetryOptions? retryOptions = null) =>
        new(
            JobKey: jobKey,
            Action: null,
            Schedule: schedule,
            JobType: jobType,
            ServiceKey: serviceKey,
            RetryOptions: retryOptions);

    private static bool VerifyBridgeArgs(HangfireJob job, Type expectedType, string? expectedServiceKey)
    {
        var expectedTypeName = expectedType.AssemblyQualifiedName;
        if (expectedTypeName is null)
        {
            return false;
        }

        return job.Args.Count == 4
            && job.Args[0] is string actualTypeName && actualTypeName == expectedTypeName
            && ((job.Args[1] is null && expectedServiceKey is null)
                || (job.Args[1] is string actualServiceKey && actualServiceKey == expectedServiceKey))
            && (job.Args[2] == null || typeof(RetryOptions).IsAssignableFrom(job.Args[2]?.GetType() ?? typeof(object)))
            && job.Args[3] is CancellationToken;
    }

    /// <summary>
    /// Открытый generic-тип, чей параметр <c>T</c> имеет <c>AssemblyQualifiedName == null</c>.
    /// </summary>
    private sealed class GenericHolder<T>
    {
    }

    /// <summary>
    /// «Неизвестный» подтип <see cref="JobSchedule"/> — производный record для проверки
    /// ветки <c>default</c> в <see cref="HangfireJobScheduler"/>.
    /// </summary>
    private sealed record UnknownJobSchedule : JobSchedule;
}