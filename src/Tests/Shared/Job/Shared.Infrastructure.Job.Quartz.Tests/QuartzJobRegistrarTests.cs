using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Shared.Application.Core.Job;

namespace Shared.Infrastructure.Job.Quartz.Tests;

public sealed class QuartzJobRegistrarTests
{
    [Fact]
    public void RegisterJob_WithCronExpression_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        var initialCount = services.Count;

        var result = services.RegisterJob("testJob", "0 0/5 * * * ?", (_, _) => Task.CompletedTask);

        result.Should().BeSameAs(services);
        services.Count.Should().BeGreaterThan(initialCount);
    }

    [Fact]
    public void RegisterJob_WithTriggerFlags_Daily_RegistersWithoutException()
    {
        var services = new ServiceCollection();
        var initialCount = services.Count;

        var act = () => services.RegisterJob(
            "dailyJob",
            JobTriggerFlags.Daily,
            (_, _) => Task.CompletedTask,
            TimeSpan.FromHours(2));

        act.Should().NotThrow();
        services.Count.Should().BeGreaterThan(initialCount);
    }

    [Fact]
    public void RegisterJob_WithTriggerFlags_EveryMinute_RegistersWithoutException()
    {
        var services = new ServiceCollection();
        var initialCount = services.Count;

        var act = () => services.RegisterJob(
            "minuteJob",
            JobTriggerFlags.EveryMinute,
            (_, _) => Task.CompletedTask,
            TimeSpan.Zero);

        act.Should().NotThrow();
        services.Count.Should().BeGreaterThan(initialCount);
    }

    [Fact]
    public void RegisterJob_WithTriggerFlags_OnStartup_RegistersWithoutException()
    {
        var services = new ServiceCollection();
        var initialCount = services.Count;

        var act = () => services.RegisterJob(
            "startupJob",
            JobTriggerFlags.OnStartup,
            (_, _) => Task.CompletedTask,
            TimeSpan.Zero);

        act.Should().NotThrow();
        services.Count.Should().BeGreaterThan(initialCount);
    }

    [Fact]
    public void RegisterJob_GenericType_RegistersTypedJob()
    {
        var services = new ServiceCollection();
        var initialCount = services.Count;

        services.RegisterJob<TestJob>("0 * * * * ?");

        services.Count.Should().BeGreaterThan(initialCount);
    }

    [Fact]
    public void RegisterCacheJob_WithCronExpression_RegistersCacheAndJob()
    {
        var services = new ServiceCollection();
        var initialCount = services.Count;

        var result = services.RegisterCacheJob<string>(
            "testCache",
            "0 * * * * ?",
            _ => Task.FromResult("cached"));

        result.Should().BeSameAs(services);
        services.Count.Should().BeGreaterThan(initialCount);
    }

    [Fact]
    public void RegisterJob_EmptyCronExpression_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var act = () => services.RegisterJob("testJob", "  ", (_, _) => Task.CompletedTask);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cronExpression");
    }

    [Fact]
    public void RegisterJob_NullJobAction_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var act = () => services.RegisterJob("testJob", "0 0/5 * * * ?", null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("job");
    }

    [Fact]
    public void RegisterJob_EmptyJobKey_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var act = () => services.RegisterJob("  ", "0 0/5 * * * ?", (_, _) => Task.CompletedTask);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("jobKey");
    }

    [Fact]
    public void RegisterJob_GenericType_WithTriggerFlags_RegistersWithoutException()
    {
        var services = new ServiceCollection();
        var initialCount = services.Count;

        var act = () => services.RegisterJob<TestJob>(
            JobTriggerFlags.Daily | JobTriggerFlags.OnStartup,
            TimeSpan.FromHours(6));

        act.Should().NotThrow();
        services.Count.Should().BeGreaterThan(initialCount);
    }

    private sealed class TestJob : IJob
    {
        public Task Execute(IJobExecutionContext context) => Task.CompletedTask;
    }
}
