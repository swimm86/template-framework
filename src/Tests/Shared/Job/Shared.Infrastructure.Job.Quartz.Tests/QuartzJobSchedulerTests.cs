// ----------------------------------------------------------------------------------------------
// <copyright file="QuartzJobSchedulerTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Shared.Application.Core.Job.Enums;
using Shared.Application.Core.Job.Scheduler;
using Shared.Infrastructure.Job.Quartz.Tests.Fakes;
using Shared.Testing.Doubles.Logging;
using Shared.Testing.Job;

namespace Shared.Infrastructure.Job.Quartz.Tests;

/// <summary>
/// Тесты <see cref="QuartzJobScheduler"/>: проверка преобразования
/// <see cref="JobSchedule"/> → <see cref="ITrigger"/> и наполнения <see cref="JobDataMap"/>.
/// </summary>
public sealed class QuartzJobSchedulerTests
{
    /// <summary>
    /// CRON-расписание: <c>0 0/5 * * * ?</c> порождает <see cref="ICronTrigger"/>
    /// с тем же выражением; имя триггера — <c>{JobKey}.trigger</c>.
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_Cron_BuildsCronTriggerWithExpectedIdentity()
    {
        // Arrange
        const string expression = "0 0/5 * * * ?";
        var definition = new JobDefinition(
            JobKey: "cronJob",
            Action: (_, _) => Task.CompletedTask,
            Schedule: new JobSchedule.Cron(expression));

        var (factory, mock) = NewFactoryWithSchedulerMock();
        var sut = NewScheduler(factory);

        // Act
        await sut.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        var call = mock.Invocations
            .Where(i => i.Method.Name == nameof(IScheduler.ScheduleJob))
            .Select(i => new ScheduleJobCall(
                (IJobDetail)i.Arguments[0],
                (ITrigger)i.Arguments[1],
                (CancellationToken)i.Arguments[2]))
            .Should().ContainSingle().Subject;

        call.Job.Key.Name.Should().Be("cronJob");
        call.Trigger.Key.Name.Should().Be("cronJob.trigger");

        var cron = call.Trigger.Should().BeAssignableTo<ICronTrigger>().Subject;
        cron.CronExpressionString.Should().Be(expression);
    }

    /// <summary>
    /// <see cref="JobSchedule.OnStartup"/> создаёт триггер с <c>StartNow()</c>;
    /// время старта близко к моменту вызова (допуск 5 секунд).
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_OnStartup_TriggersStartNow()
    {
        // Arrange
        var definition = new JobDefinition(
            JobKey: "onStart",
            Action: (_, _) => Task.CompletedTask,
            Schedule: new JobSchedule.OnStartup());

        var (factory, mock) = NewFactoryWithSchedulerMock();
        var sut = NewScheduler(factory);
        var before = DateTimeOffset.UtcNow;

        // Act
        await sut.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        var trigger = mock.Invocations
            .Where(i => i.Method.Name == nameof(IScheduler.ScheduleJob))
            .Select(i => (ITrigger)i.Arguments[1])
            .Should().ContainSingle().Subject;

        trigger.StartTimeUtc.Should().BeOnOrAfter(before.AddSeconds(-1));
        trigger.StartTimeUtc.Should().BeOnOrBefore(DateTimeOffset.UtcNow.AddSeconds(1));
    }

    /// <summary>
    /// <see cref="JobTriggerFlags.Daily"/> + конкретное время: старт в указанный час/минуту,
    /// интервал — 1 день (<see cref="IntervalUnit.Day"/>).
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_FlagsDailyAt2Am_TriggersStartAtGivenTime()
    {
        // Arrange
        var definition = new JobDefinition(
            JobKey: "dailyJob",
            Action: (_, _) => Task.CompletedTask,
            Schedule: new JobSchedule.Flags(JobTriggerFlags.Daily, TimeSpan.FromHours(2)));

        var (factory, mock) = NewFactoryWithSchedulerMock();
        var sut = NewScheduler(factory);

        // Act
        await sut.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        var trigger = ExtractSingleTrigger(mock);
        var calendar = trigger.Should().BeAssignableTo<ICalendarIntervalTrigger>().Subject;

        calendar.RepeatInterval.Should().Be(1);
        calendar.RepeatIntervalUnit.Should().Be(IntervalUnit.Day);

        // DateBuilder.DateOf(2,0,0) → сегодняшняя дата с Kind=Unspecified.
        // Quartz конвертирует в UTC → StartTimeUtc = 02:00 в локальной TZ.
        var startLocal = trigger.StartTimeUtc.ToLocalTime();
        startLocal.Hour.Should().Be(2);
        startLocal.Minute.Should().Be(0);
        startLocal.Second.Should().Be(0);
    }

    /// <summary>
    /// <see cref="JobTriggerFlags.EveryMinute"/> → интервал 1 минута.
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_FlagsEveryMinute_TriggersEveryMinute()
    {
        // Arrange
        var definition = new JobDefinition(
            JobKey: "everyMin",
            Action: (_, _) => Task.CompletedTask,
            Schedule: new JobSchedule.Flags(JobTriggerFlags.EveryMinute, TimeSpan.Zero));

        var (factory, mock) = NewFactoryWithSchedulerMock();
        var sut = NewScheduler(factory);

        // Act
        await sut.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        var calendar = ExtractSingleTrigger(mock)
            .Should().BeAssignableTo<ICalendarIntervalTrigger>().Subject;
        calendar.RepeatInterval.Should().Be(1);
        calendar.RepeatIntervalUnit.Should().Be(IntervalUnit.Minute);
    }

    /// <summary>
    /// <see cref="JobTriggerFlags.EveryHour"/> → интервал 1 час.
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_FlagsEveryHour_TriggersEveryHour()
    {
        // Arrange
        var definition = new JobDefinition(
            JobKey: "everyHour",
            Action: (_, _) => Task.CompletedTask,
            Schedule: new JobSchedule.Flags(JobTriggerFlags.EveryHour, TimeSpan.Zero));

        var (factory, mock) = NewFactoryWithSchedulerMock();
        var sut = NewScheduler(factory);

        // Act
        await sut.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        var calendar = ExtractSingleTrigger(mock)
            .Should().BeAssignableTo<ICalendarIntervalTrigger>().Subject;
        calendar.RepeatInterval.Should().Be(1);
        calendar.RepeatIntervalUnit.Should().Be(IntervalUnit.Hour);
    }

    /// <summary>
    /// <see cref="JobTriggerFlags.Weekly"/> → интервал 1 неделя.
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_FlagsWeekly_TriggersEveryWeek()
    {
        // Arrange
        var definition = new JobDefinition(
            JobKey: "weekly",
            Action: (_, _) => Task.CompletedTask,
            Schedule: new JobSchedule.Flags(JobTriggerFlags.Weekly, TimeSpan.FromHours(9)));

        var (factory, mock) = NewFactoryWithSchedulerMock();
        var sut = NewScheduler(factory);

        // Act
        await sut.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        var calendar = ExtractSingleTrigger(mock)
            .Should().BeAssignableTo<ICalendarIntervalTrigger>().Subject;
        calendar.RepeatInterval.Should().Be(1);
        calendar.RepeatIntervalUnit.Should().Be(IntervalUnit.Week);
    }

    /// <summary>
    /// <see cref="JobTriggerFlags.Monthly"/> → интервал 1 месяц.
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_FlagsMonthly_TriggersEveryMonth()
    {
        // Arrange
        var definition = new JobDefinition(
            JobKey: "monthly",
            Action: (_, _) => Task.CompletedTask,
            Schedule: new JobSchedule.Flags(JobTriggerFlags.Monthly, TimeSpan.FromHours(8)));

        var (factory, mock) = NewFactoryWithSchedulerMock();
        var sut = NewScheduler(factory);

        // Act
        await sut.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        var calendar = ExtractSingleTrigger(mock)
            .Should().BeAssignableTo<ICalendarIntervalTrigger>().Subject;
        calendar.RepeatInterval.Should().Be(1);
        calendar.RepeatIntervalUnit.Should().Be(IntervalUnit.Month);
    }

    /// <summary>
    /// <see cref="JobTriggerFlags.OnStartup"/> внутри <see cref="JobSchedule.Flags"/>:
    /// триггер с <c>StartNow()</c>.
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_FlagsOnStartup_TriggersStartNow()
    {
        // Arrange
        var definition = new JobDefinition(
            JobKey: "flagsStartup",
            Action: (_, _) => Task.CompletedTask,
            Schedule: new JobSchedule.Flags(JobTriggerFlags.OnStartup, TimeSpan.Zero));

        var (factory, mock) = NewFactoryWithSchedulerMock();
        var sut = NewScheduler(factory);
        var before = DateTimeOffset.UtcNow;

        // Act
        await sut.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        var trigger = ExtractSingleTrigger(mock);
        trigger.StartTimeUtc.Should().BeOnOrAfter(before.AddSeconds(-1));
        trigger.StartTimeUtc.Should().BeOnOrBefore(DateTimeOffset.UtcNow.AddSeconds(1));
    }

    /// <summary>
    /// Комбинация <c>Daily | OnStartup</c> в <see cref="JobSchedule.Flags"/>:
    /// Quartz создаёт один триггер с интервалом Daily (OnStartup трактуется как
    /// запуск сразу + ежедневное повторение, что разумно для warm-up сценариев).
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_FlagsDailyOrOnStartup_BuildsSingleDailyTrigger()
    {
        // Arrange
        var definition = new JobDefinition(
            JobKey: "dailyOrStartup",
            Action: (_, _) => Task.CompletedTask,
            Schedule: new JobSchedule.Flags(
                JobTriggerFlags.Daily | JobTriggerFlags.OnStartup,
                TimeSpan.FromHours(3)));

        var (factory, mock) = NewFactoryWithSchedulerMock();
        var sut = NewScheduler(factory);

        // Act
        await sut.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        mock.Invocations
            .Where(i => i.Method.Name == nameof(IScheduler.ScheduleJob))
            .Should().ContainSingle("OnStartup в комбинации с Daily — допустимо (только один schedule-флаг)");
        var calendar = ExtractSingleTrigger(mock)
            .Should().BeAssignableTo<ICalendarIntervalTrigger>().Subject;
        calendar.RepeatIntervalUnit.Should().Be(IntervalUnit.Day);
    }

    /// <summary>
    /// Комбинация двух schedule-флагов (<c>Daily | Weekly</c>) без <c>OnStartup</c>:
    /// <see cref="QuartzJobScheduler"/> бросает <see cref="ArgumentException"/>, так как
    /// Quartz <c>TriggerBuilder.WithCalendarIntervalSchedule</c> перезаписывает предыдущее
    /// расписание — множественные интервалы не поддерживаются.
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_FlagsDailyAndWeeklyWithoutOnStartup_ThrowsArgumentException()
    {
        // Arrange
        var definition = new JobDefinition(
            JobKey: "multiSchedule",
            Action: (_, _) => Task.CompletedTask,
            Schedule: new JobSchedule.Flags(
                JobTriggerFlags.Daily | JobTriggerFlags.Weekly,
                TimeSpan.FromHours(3)));

        var (factory, _) = NewFactoryWithSchedulerMock();
        var sut = NewScheduler(factory);

        // Act
        var act = () => sut.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*multiple interval schedules*");
    }

    /// <summary>
    /// Классовая джоба: <see cref="JobDataMap"/> содержит
    /// <see cref="QuartzScheduledJobAdapter.JobTypeKey"/> с <c>AssemblyQualifiedName</c>
    /// и НЕ содержит <see cref="JobDefinition.ActionDataKey"/>.
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_ClassJob_StoresTypeAndOmitsAction()
    {
        // Arrange
        var definition = new JobDefinition(
            JobKey: "classJob",
            Action: null,
            Schedule: new JobSchedule.Cron("0 0 * * * ?"),
            JobType: typeof(FakeScheduledJob));

        var (factory, mock) = NewFactoryWithSchedulerMock();
        var sut = NewScheduler(factory);

        // Act
        await sut.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        var data = ExtractSingleJob(mock).JobDataMap;
        data.Should().ContainKey(QuartzScheduledJobAdapter.JobTypeKey);
        data[QuartzScheduledJobAdapter.JobTypeKey].Should().Be(typeof(FakeScheduledJob).AssemblyQualifiedName);
        data.Should().NotContainKey(JobDefinition.ActionDataKey);
    }

    /// <summary>
    /// Классовая джоба + <see cref="JobDefinition.ServiceKey"/>: <see cref="JobDataMap"/>
    /// содержит оба ключа — <c>JobType</c> и <c>ServiceKey</c>.
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_ClassJobWithServiceKey_StoresBothKeys()
    {
        // Arrange
        var definition = new JobDefinition(
            JobKey: "keyedJob",
            Action: null,
            Schedule: new JobSchedule.Cron("0 0 * * * ?"),
            JobType: typeof(FakeScheduledJob),
            ServiceKey: "primary");

        var (factory, mock) = NewFactoryWithSchedulerMock();
        var sut = NewScheduler(factory);

        // Act
        await sut.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        var data = ExtractSingleJob(mock).JobDataMap;
        data[QuartzScheduledJobAdapter.JobTypeKey].Should().Be(typeof(FakeScheduledJob).AssemblyQualifiedName);
        data[QuartzScheduledJobAdapter.ServiceKeyKey].Should().Be("primary");
    }

    /// <summary>
    /// Лямбда-джоба (<see cref="JobDefinition.JobType"/> = <c>null</c>):
    /// <see cref="JobDataMap"/> содержит делегат по <see cref="JobDefinition.ActionDataKey"/>
    /// и НЕ содержит <see cref="QuartzScheduledJobAdapter.JobTypeKey"/>.
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_LambdaJob_StoresActionAndOmitsJobType()
    {
        // Arrange
        Func<IServiceProvider, CancellationToken, Task> action = (_, _) => Task.CompletedTask;
        var definition = new JobDefinition(
            JobKey: "lambdaJob",
            Action: action,
            Schedule: new JobSchedule.Cron("0 0 * * * ?"));

        var (factory, mock) = NewFactoryWithSchedulerMock();
        var sut = NewScheduler(factory);

        // Act
        await sut.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        var data = ExtractSingleJob(mock).JobDataMap;
        data.Should().NotContainKey(QuartzScheduledJobAdapter.JobTypeKey);
        data[JobDefinition.ActionDataKey].Should().BeSameAs(action);
    }

    /// <summary>
    /// Классовая джоба, чей <see cref="Type.AssemblyQualifiedName"/> равен <c>null</c>
    /// (например, открытый generic-параметр): ожидать <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_JobTypeWithoutAssemblyQualifiedName_Throws()
    {
        // Arrange
        var openGenericParam = typeof(GenericHolder<>).GetGenericArguments()[0];
        openGenericParam.AssemblyQualifiedName.Should().BeNull(
            "тест полагается на то, что у generic-параметра T нет AssemblyQualifiedName");

        var definition = new JobDefinition(
            JobKey: "noAqn",
            Action: null,
            Schedule: new JobSchedule.Cron("0 0 * * * ?"),
            JobType: openGenericParam);

        var (factory, _) = NewFactoryWithSchedulerMock();
        var sut = NewScheduler(factory);

        // Act
        var act = () => sut.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AssemblyQualifiedName*");
    }

    /// <summary>
    /// <see cref="JobDefinition"/> = <c>null</c> — <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_NullDefinition_Throws()
    {
        // Arrange
        var (factory, _) = NewFactoryWithSchedulerMock();
        var sut = NewScheduler(factory);

        // Act
        var act = () => sut.ScheduleAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Неизвестный подтип <see cref="JobSchedule"/> выбрасывает
    /// <see cref="ArgumentOutOfRangeException"/> (защита от расширения sealed-иерархии в чужом коде).
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_UnknownJobSchedule_Throws()
    {
        // Arrange
        var definition = new JobDefinition(
            JobKey: "unknownSchedule",
            Action: (_, _) => Task.CompletedTask,
            Schedule: new UnknownJobSchedule());

        var (factory, _) = NewFactoryWithSchedulerMock();
        var sut = NewScheduler(factory);

        // Act
        var act = () => sut.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// <see cref="QuartzJobScheduler"/> логирует информационное сообщение с именем
    /// <c>JobKey</c> и типом расписания.
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_LogsInformation()
    {
        // Arrange
        var logger = new FakeLogger();
        var (factory, _) = NewFactoryWithSchedulerMock();
        var sut = new QuartzJobScheduler(factory, new FakeLogger<QuartzJobScheduler>(logger));

        var definition = new JobDefinition(
            JobKey: "logged",
            Action: (_, _) => Task.CompletedTask,
            Schedule: new JobSchedule.Cron("0 0 * * * ?"));

        // Act
        await sut.ScheduleAsync(definition, TestContext.Current.CancellationToken);

        // Assert
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Information);
        logger.Entries.Single().Message.Should().Contain("logged");
        logger.Entries.Single().Message.Should().Contain("Cron");
    }

    /// <summary>
    /// <see cref="CancellationToken"/> из вызова пробрасывается в
    /// <see cref="ISchedulerFactory.GetScheduler(CancellationToken)"/> и в
    /// <c>IScheduler.ScheduleJob</c>.
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_ForwardsCancellationToken()
    {
        // Arrange
        var definition = new JobDefinition(
            JobKey: "ct",
            Action: (_, _) => Task.CompletedTask,
            Schedule: new JobSchedule.Cron("0 0 * * * ?"));

        var (factory, mock) = NewFactoryWithSchedulerMock();
        var sut = NewScheduler(factory);
        using var cts = new CancellationTokenSource();

        // Act
        await sut.ScheduleAsync(definition, cts.Token);

        // Assert
        factory.GetSchedulerCalls.Should().ContainSingle().Which.Should().Be(cts.Token);
        var scheduleCall = mock.Invocations
            .Where(i => i.Method.Name == nameof(IScheduler.ScheduleJob))
            .Should().ContainSingle().Subject;
        scheduleCall.Arguments[2].Should().Be(cts.Token);
    }

    private static QuartzJobScheduler NewScheduler(ISchedulerFactory factory) =>
        new(factory, new FakeLogger<QuartzJobScheduler>(new FakeLogger()));

    private static (FakeSchedulerFactory Factory, Mock<IScheduler> Mock) NewFactoryWithSchedulerMock()
    {
        var factory = new FakeSchedulerFactory();
        factory.SchedulerMock
            .Setup(s => s.ScheduleJob(
                It.IsAny<IJobDetail>(),
                It.IsAny<ITrigger>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(DateTimeOffset.UtcNow));
        return (factory, factory.SchedulerMock);
    }

    private static ITrigger ExtractSingleTrigger(Mock<IScheduler> mock)
    {
        var call = mock.Invocations
            .Where(i => i.Method.Name == nameof(IScheduler.ScheduleJob))
            .Should().ContainSingle().Subject;
        return (ITrigger)call.Arguments[1];
    }

    private static IJobDetail ExtractSingleJob(Mock<IScheduler> mock)
    {
        var call = mock.Invocations
            .Where(i => i.Method.Name == nameof(IScheduler.ScheduleJob))
            .Should().ContainSingle().Subject;
        return (IJobDetail)call.Arguments[0];
    }

    /// <summary>
    /// Открытый generic-тип, чей параметр <c>T</c> имеет <c>AssemblyQualifiedName == null</c>.
    /// </summary>
    private sealed class GenericHolder<T>
    {
    }

    /// <summary>
    /// «Неизвестный» подтип <see cref="JobSchedule"/> — производный record для проверки
    /// ветки <c>default</c> в <see cref="QuartzJobScheduler"/>.
    /// </summary>
    private sealed record UnknownJobSchedule : JobSchedule;

    /// <summary>
    /// Аргументы вызова <see cref="IScheduler.ScheduleJob(IJobDetail, ITrigger, CancellationToken)"/>.
    /// Используется только в одном тесте для удобства; в остальных извлекаем напрямую
    /// из <c>mock.Invocations</c>.
    /// </summary>
    private sealed record ScheduleJobCall(IJobDetail Job, ITrigger Trigger, CancellationToken Token);
}
