// ----------------------------------------------------------------------------------------------
// <copyright file="JobSchedulerBuilderTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Job.Enums;
using Shared.Application.Core.Job.Interfaces;
using Shared.Application.Core.Job.Pipeline;
using Shared.Application.Core.Job.Scheduler;
using Shared.Application.Core.Tests.Support;

namespace Shared.Application.Core.Tests.Job;

/// <summary>
/// Тесты для <see cref="JobSchedulerBuilder"/>: добавление разных типов джоб, валидация
/// пустого ключа, конвертация <see cref="JobTriggerFlags"/> в <see cref="JobSchedule.Flags"/>,
/// проброс <see cref="RetryOptions"/> во все перегрузки регистрации.
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
    /// Non-generic-перегрузка <c>AddJob(Type, string serviceKey, JobSchedule, RetryOptions)</c>
    /// с <c>null</c> <c>serviceKey</c> выбрасывает <see cref="ArgumentNullException"/>
    /// по параметру <c>serviceKey</c>.
    /// <para>
    /// Контракт: пустая строка допустима (как и в keyed-резолве DI), а <c>null</c> — нет.
    /// Покрывает как generic-, так и non-generic-перегрузки единым правилом.
    /// </para>
    /// </summary>
    [Fact]
    public void AddJob_NonGenericWithNullServiceKey_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new JobSchedulerBuilder();

        // Act
        var act = () => builder.AddJob(
            jobType: typeof(FakeClassJob),
            serviceKey: null!,
            schedule: new JobSchedule.OnStartup());

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceKey");
    }

    /// <summary>
    /// Generic-перегрузка <c>AddJob&lt;TJob&gt;(JobSchedule, RetryOptions)</c>
    /// сохраняет <see cref="RetryOptions"/> в <see cref="JobDefinition"/>.
    /// </summary>
    [Fact]
    public void AddJob_GenericWithRetryOptions_StoresRetryOptionsInDefinition()
    {
        // Arrange
        var builder = new JobSchedulerBuilder();
        var retryOptions = RetryTestSupport.DefaultOptions();

        // Act
        builder.AddJob<FakeClassJob>(new JobSchedule.Cron("0 0 * * * ?"), retryOptions);

        // Assert
        var definition = builder.Definitions.Should().ContainSingle().Subject;
        definition.RetryOptions.Should().BeSameAs(retryOptions);
    }

    /// <summary>
    /// Generic-перегрузка без <see cref="RetryOptions"/> оставляет в определении <c>null</c>.
    /// </summary>
    [Fact]
    public void AddJob_GenericWithoutRetryOptions_LeavesRetryOptionsNull()
    {
        // Arrange
        var builder = new JobSchedulerBuilder();

        // Act
        builder.AddJob<FakeClassJob>(new JobSchedule.Cron("0 0 * * * ?"));

        // Assert
        builder.Definitions.Should().ContainSingle()
            .Which.RetryOptions.Should().BeNull();
    }

    /// <summary>
    /// Generic-перегрузка <c>AddJob&lt;TJob&gt;(string serviceKey, JobSchedule, RetryOptions)</c>
    /// формирует ключ вида <c>{FullName}#{serviceKey}</c> и сохраняет
    /// <see cref="JobDefinition.ServiceKey"/>.
    /// </summary>
    [Fact]
    public void AddJob_GenericWithServiceKey_BuildsCompositeJobKey()
    {
        // Arrange
        var builder = new JobSchedulerBuilder();
        var retryOptions = RetryTestSupport.DefaultOptions();

        // Act
        builder.AddJob<FakeClassJob>("primary", new JobSchedule.OnStartup(), retryOptions);

        // Assert
        var definition = builder.Definitions.Should().ContainSingle().Subject;
        definition.JobKey.Should().Be($"{typeof(FakeClassJob).FullName}#primary");
        definition.ServiceKey.Should().Be("primary");
        definition.JobType.Should().Be<FakeClassJob>();
        definition.RetryOptions.Should().BeSameAs(retryOptions);
    }

    /// <summary>
    /// Generic-перегрузка с <c>null</c> <c>serviceKey</c> выбрасывает <see cref="ArgumentNullException"/>.
    /// <para>
    /// Контракт: <see cref="string.Empty"/> допустим (как и в keyed-резолве DI),
    /// а вот <c>null</c> — нет.
    /// </para>
    /// </summary>
    [Fact]
    public void AddJob_GenericWithNullServiceKey_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new JobSchedulerBuilder();

        // Act
        var act = () => builder.AddJob<FakeClassJob>(
            serviceKey: null!,
            schedule: new JobSchedule.OnStartup());

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceKey");
    }

    /// <summary>
    /// <c>AddJob(Type, JobSchedule, RetryOptions)</c> сохраняет
    /// <see cref="RetryOptions"/> и оставляет <c>ServiceKey = null</c>.
    /// </summary>
    [Fact]
    public void AddJob_TypeWithRetryOptions_StoresRetryOptionsInDefinition()
    {
        // Arrange
        var builder = new JobSchedulerBuilder();
        var retryOptions = RetryTestSupport.DefaultOptions();

        // Act
        builder.AddJob(typeof(FakeClassJob), new JobSchedule.OnStartup(), retryOptions);

        // Assert
        var definition = builder.Definitions.Should().ContainSingle().Subject;
        definition.RetryOptions.Should().BeSameAs(retryOptions);
        definition.ServiceKey.Should().BeNull();
        definition.JobType.Should().Be<FakeClassJob>();
    }

    /// <summary>
    /// Лямбда-перегрузка <c>AddJob(string, JobSchedule, Func, RetryOptions)</c>
    /// сохраняет <see cref="RetryOptions"/> в <see cref="JobDefinition"/>.
    /// </summary>
    [Fact]
    public void AddJob_LambdaWithRetryOptions_StoresRetryOptionsInDefinition()
    {
        // Arrange
        var builder = new JobSchedulerBuilder();
        var retryOptions = RetryTestSupport.DefaultOptions();

        // Act
        builder.AddJob("lambda", new JobSchedule.Cron("0 0 * * * ?"), (_, _) => Task.CompletedTask, retryOptions);

        // Assert
        var definition = builder.Definitions.Should().ContainSingle().Subject;
        definition.RetryOptions.Should().BeSameAs(retryOptions);
    }

    /// <summary>
    /// <c>AddCron(..., RetryOptions)</c> пробрасывает <see cref="RetryOptions"/> дальше
    /// в базовую <c>AddJob</c> и сохраняет в определении.
    /// </summary>
    [Fact]
    public void AddCron_WithRetryOptions_StoresRetryOptionsInDefinition()
    {
        // Arrange
        var builder = new JobSchedulerBuilder();
        var retryOptions = RetryTestSupport.DefaultOptions();

        // Act
        builder.AddCron("cron-job", "0 0 * * * ?", (_, _) => Task.CompletedTask, retryOptions);

        // Assert
        var definition = builder.Definitions.Should().ContainSingle().Subject;
        definition.RetryOptions.Should().BeSameAs(retryOptions);
    }

    /// <summary>
    /// <c>AddFlags(..., RetryOptions)</c> пробрасывает <see cref="RetryOptions"/> дальше
    /// в базовую <c>AddJob</c> и сохраняет в определении.
    /// </summary>
    [Fact]
    public void AddFlags_WithRetryOptions_StoresRetryOptionsInDefinition()
    {
        // Arrange
        var builder = new JobSchedulerBuilder();
        var retryOptions = RetryTestSupport.DefaultOptions();

        // Act
        builder.AddFlags(
            "flags-job",
            JobTriggerFlags.Daily,
            TimeSpan.FromHours(2),
            (_, _) => Task.CompletedTask,
            retryOptions);

        // Assert
        var definition = builder.Definitions.Should().ContainSingle().Subject;
        definition.RetryOptions.Should().BeSameAs(retryOptions);
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
