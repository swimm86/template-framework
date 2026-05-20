using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Shared.Application.Core.Job;

namespace Shared.Infrastructure.Job.Quartz.Tests;

/// <summary>
/// Модульные тесты для методов расширения регистрации Quartz заданий.
/// </summary>
public sealed class QuartzJobRegistrarTests
{
    /// <summary>
    /// Регистрация задания с Cron-выражением возвращает ту же коллекцию сервисов.
    /// </summary>
    [Fact]
    public void RegisterJob_WithCronExpression_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var initialCount = services.Count;

        // Act
        var result = services.RegisterJob("testJob", "0 0/5 * * * ?", (_, _) => Task.CompletedTask);

        // Assert
        result.Should().BeSameAs(services);
        services.Count.Should().BeGreaterThan(initialCount);
    }

    /// <summary>
    /// Регистрация задания с флагом Daily выполняется без исключений.
    /// </summary>
    [Fact]
    public void RegisterJob_WithTriggerFlags_Daily_RegistersWithoutException()
    {
        // Arrange
        var services = new ServiceCollection();
        var initialCount = services.Count;

        // Act
        var act = () => services.RegisterJob(
            "dailyJob",
            JobTriggerFlags.Daily,
            (_, _) => Task.CompletedTask,
            TimeSpan.FromHours(2));

        // Assert
        act.Should().NotThrow();
        services.Count.Should().BeGreaterThan(initialCount);
    }

    /// <summary>
    /// Регистрация задания с флагом EveryMinute выполняется без исключений.
    /// </summary>
    [Fact]
    public void RegisterJob_WithTriggerFlags_EveryMinute_RegistersWithoutException()
    {
        // Arrange
        var services = new ServiceCollection();
        var initialCount = services.Count;

        // Act
        var act = () => services.RegisterJob(
            "minuteJob",
            JobTriggerFlags.EveryMinute,
            (_, _) => Task.CompletedTask,
            TimeSpan.Zero);

        // Assert
        act.Should().NotThrow();
        services.Count.Should().BeGreaterThan(initialCount);
    }

    /// <summary>
    /// Регистрация задания с флагом OnStartup выполняется без исключений.
    /// </summary>
    [Fact]
    public void RegisterJob_WithTriggerFlags_OnStartup_RegistersWithoutException()
    {
        // Arrange
        var services = new ServiceCollection();
        var initialCount = services.Count;

        // Act
        var act = () => services.RegisterJob(
            "startupJob",
            JobTriggerFlags.OnStartup,
            (_, _) => Task.CompletedTask,
            TimeSpan.Zero);

        // Assert
        act.Should().NotThrow();
        services.Count.Should().BeGreaterThan(initialCount);
    }

    /// <summary>
    /// Регистрация типизированного задания регистрирует задание указанного типа.
    /// </summary>
    [Fact]
    public void RegisterJob_GenericType_RegistersTypedJob()
    {
        // Arrange
        var services = new ServiceCollection();
        var initialCount = services.Count;

        // Act
        services.RegisterJob<TestJob>("0 * * * * ?");

        // Assert
        services.Count.Should().BeGreaterThan(initialCount);
    }

    /// <summary>
    /// Регистрация кэш-задания с Cron-выражением регистрирует и кэш, и задание.
    /// </summary>
    [Fact]
    public void RegisterCacheJob_WithCronExpression_RegistersCacheAndJob()
    {
        // Arrange
        var services = new ServiceCollection();
        var initialCount = services.Count;

        // Act
        var result = services.RegisterCacheJob<string>(
            "testCache",
            "0 * * * * ?",
            _ => Task.FromResult("cached"));

        // Assert
        result.Should().BeSameAs(services);
        services.Count.Should().BeGreaterThan(initialCount);
    }

    /// <summary>
    /// Пустое Cron-выражение вызывает ArgumentNullException.
    /// </summary>
    [Fact]
    public void RegisterJob_EmptyCronExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.RegisterJob("testJob", "  ", (_, _) => Task.CompletedTask);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cronExpression");
    }

    /// <summary>
    /// Null-делегат задания вызывает ArgumentNullException.
    /// </summary>
    [Fact]
    public void RegisterJob_NullJobAction_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.RegisterJob("testJob", "0 0/5 * * * ?", null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("job");
    }

    /// <summary>
    /// Пустой ключ задания вызывает ArgumentNullException.
    /// </summary>
    [Fact]
    public void RegisterJob_EmptyJobKey_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.RegisterJob("  ", "0 0/5 * * * ?", (_, _) => Task.CompletedTask);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("jobKey");
    }

    /// <summary>
    /// Типизированное задание с флагами триггера регистрируется без исключений.
    /// </summary>
    [Fact]
    public void RegisterJob_GenericType_WithTriggerFlags_RegistersWithoutException()
    {
        // Arrange
        var services = new ServiceCollection();
        var initialCount = services.Count;

        // Act
        var act = () => services.RegisterJob<TestJob>(
            JobTriggerFlags.Daily | JobTriggerFlags.OnStartup,
            TimeSpan.FromHours(6));

        // Assert
        act.Should().NotThrow();
        services.Count.Should().BeGreaterThan(initialCount);
    }

    /// <summary>
    /// Тестовое Quartz-задание без логики.
    /// </summary>
    private sealed class TestJob : IJob
    {
        public Task Execute(IJobExecutionContext context) => Task.CompletedTask;
    }
}
