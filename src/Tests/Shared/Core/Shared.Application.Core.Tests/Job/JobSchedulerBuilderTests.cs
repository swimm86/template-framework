// ----------------------------------------------------------------------------------------------
// <copyright file="JobSchedulerBuilderTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Job.Enums;
using Shared.Application.Core.Job.Interfaces;
using Shared.Application.Core.Job.Scheduler;

namespace Shared.Application.Core.Tests.Job;

/// <summary>
/// Тесты для <see cref="JobSchedulerBuilder"/>: добавление разных типов джоб, валидация
/// пустого ключа, конвертация <see cref="JobTriggerFlags"/> в <see cref="JobSchedule.Flags"/>.
/// </summary>
public sealed class JobSchedulerBuilderTests
{
    /// <summary>
    /// Пустой ключ джобы выбрасывает <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void AddJob_EmptyKey_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new JobSchedulerBuilder();

        // Act
        var act = () => builder.AddJob(string.Empty, new JobSchedule.Cron("0 0 * * * ?"), (_, _) => Task.CompletedTask);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("jobKey");
    }

    /// <summary>
    /// Добавление трёх разных типов джоб (лямбда, generic-класс, type-класс) даёт три определения.
    /// </summary>
    [Fact]
    public void AddJob_AllTypes_AddsThreeDefinitions()
    {
        // Arrange
        var builder = new JobSchedulerBuilder();

        // Act
        builder
            .AddCron("lambda", "0 0 * * * ?", (_, _) => Task.CompletedTask)
            .AddJob<FakeClassJob>(new JobSchedule.Cron("0 0 * * * ?"))
            .AddJob(typeof(FakeClassJob), new JobSchedule.OnStartup());

        // Assert
        builder.Definitions.Should().HaveCount(3);
    }

    /// <summary>
    /// <see cref="JobTriggerFlags"/> правильно конвертируется в <see cref="JobSchedule.Flags"/>
    /// при добавлении через <see cref="JobSchedulerBuilder.AddFlags"/>.
    /// </summary>
    [Fact]
    public void AddFlags_StoresFlagsInSchedule()
    {
        // Arrange
        var builder = new JobSchedulerBuilder();
        var time = TimeSpan.FromHours(3);

        // Act
        builder.AddFlags(
            "dailyJob",
            JobTriggerFlags.Daily | JobTriggerFlags.Weekly,
            time,
            (_, _) => Task.CompletedTask);

        // Assert
        var definition = builder.Definitions.Should().ContainSingle().Subject;
        var schedule = definition.Schedule.Should().BeOfType<JobSchedule.Flags>().Subject;
        schedule.TriggerFlags.Should().HaveFlag(JobTriggerFlags.Daily);
        schedule.TriggerFlags.Should().HaveFlag(JobTriggerFlags.Weekly);
        schedule.SpecificTime.Should().Be(time);
    }

    /// <summary>
    /// Пустое CRON-выражение выбрасывает <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void AddCron_EmptyExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new JobSchedulerBuilder();

        // Act
        var act = () => builder.AddCron("job", "  ", (_, _) => Task.CompletedTask);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cronExpression");
    }

    /// <summary>
    /// Добавление классовой джобы с типом, не реализующим <see cref="IScheduledJob"/>,
    /// выбрасывает <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void AddJob_TypeNotImplementingIScheduledJob_ThrowsArgumentException()
    {
        // Arrange
        var builder = new JobSchedulerBuilder();

        // Act
        var act = () => builder.AddJob(typeof(string), new JobSchedule.OnStartup());

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("jobType");
    }

    /// <summary>
    /// Тестовая классовая джоба.
    /// </summary>
    private sealed class FakeClassJob
        : IScheduledJob
    {
        public Task ExecuteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
