using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Shared.Application.Core.CorrelationId;
using Shared.Infrastructure.Job.Quartz.Tests.Infrastructure;
using Shared.Testing.Doubles.Logging;

namespace Shared.Infrastructure.Job.Quartz.Tests;

public sealed class QuartzJobWrapperTests : IDisposable
{
    private readonly FakeLogger _fakeLogger;
    private readonly FakeLogger<QuartzJobWrapper> _logger;
    private readonly IServiceProvider _serviceProvider;

    public QuartzJobWrapperTests()
    {
        _fakeLogger = new FakeLogger();
        _logger = new FakeLogger<QuartzJobWrapper>(_fakeLogger);
        _serviceProvider = new ServiceCollection().BuildServiceProvider();
        JobCorrelationContext.ClearCorrelationId();
    }

    public void Dispose()
    {
        JobCorrelationContext.ClearCorrelationId();
    }

    [Fact]
    public async Task Execute_HappyPath_InvokesDelegateFromJobDataMap()
    {
        var invoked = false;
        Func<IServiceProvider, CancellationToken, Task> jobAction = (_, _) =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        var jobDetail = new StubJobDetail
        {
            JobDataMap = new JobDataMap { { QuartzJobWrapper.JobActionKey, jobAction } }
        };

        var context = new StubJobExecutionContext
        {
            Scheduler = new StubScheduler(),
            JobDetail = jobDetail,
            Trigger = new StubTrigger(),
            CancellationToken = CancellationToken.None
        };

        var wrapper = new QuartzJobWrapper(_serviceProvider, _logger);

        await wrapper.Execute(context);

        invoked.Should().BeTrue();
    }

    [Fact]
    public async Task Execute_HappyPath_DoesNotScheduleRetry()
    {
        var jobAction = (IServiceProvider _, CancellationToken _) => Task.CompletedTask;
        var scheduler = new StubScheduler();

        var jobDetail = new StubJobDetail
        {
            JobDataMap = new JobDataMap { { QuartzJobWrapper.JobActionKey, jobAction } }
        };

        var context = new StubJobExecutionContext
        {
            Scheduler = scheduler,
            JobDetail = jobDetail,
            Trigger = new StubTrigger(),
            CancellationToken = CancellationToken.None
        };

        var wrapper = new QuartzJobWrapper(_serviceProvider, _logger);

        await wrapper.Execute(context);

        scheduler.ScheduleJobCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Execute_DelegateThrows_SchedulesRetryAndThrowsJobExecutionException()
    {
        var exception = new InvalidOperationException("test error");
        Func<IServiceProvider, CancellationToken, Task> jobAction = (_, _) => throw exception;

        var scheduler = new StubScheduler();
        var jobDetail = new StubJobDetail
        {
            Key = new JobKey("test-job"),
            JobDataMap = new JobDataMap { { QuartzJobWrapper.JobActionKey, jobAction } }
        };

        var context = new StubJobExecutionContext
        {
            Scheduler = scheduler,
            JobDetail = jobDetail,
            Trigger = new StubTrigger(),
            CancellationToken = CancellationToken.None
        };

        var wrapper = new QuartzJobWrapper(_serviceProvider, _logger);

        var act = () => wrapper.Execute(context);

        var ex = (await act.Should().ThrowAsync<JobExecutionException>()).Which;
        ex.InnerException.Should().Be(exception);
        ex.RefireImmediately.Should().BeFalse();

        scheduler.ScheduleJobCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Execute_PreservesCancellationToken()
    {
        CancellationToken passedToken = default;
        Func<IServiceProvider, CancellationToken, Task> jobAction = (_, ct) =>
        {
            passedToken = ct;
            return Task.CompletedTask;
        };

        using var cts = new CancellationTokenSource();
        var jobDetail = new StubJobDetail
        {
            JobDataMap = new JobDataMap { { QuartzJobWrapper.JobActionKey, jobAction } }
        };

        var context = new StubJobExecutionContext
        {
            Scheduler = new StubScheduler(),
            JobDetail = jobDetail,
            Trigger = new StubTrigger(),
            CancellationToken = cts.Token
        };

        var wrapper = new QuartzJobWrapper(_serviceProvider, _logger);

        await wrapper.Execute(context);

        passedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task Execute_SetsAndClearsCorrelationId()
    {
        var jobAction = (IServiceProvider _, CancellationToken _) => Task.CompletedTask;

        var jobDetail = new StubJobDetail
        {
            JobDataMap = new JobDataMap { { QuartzJobWrapper.JobActionKey, jobAction } }
        };

        var context = new StubJobExecutionContext
        {
            Scheduler = new StubScheduler(),
            JobDetail = jobDetail,
            Trigger = new StubTrigger(),
            CancellationToken = CancellationToken.None
        };

        var wrapper = new QuartzJobWrapper(_serviceProvider, _logger);

        JobCorrelationContext.GetCorrelationId().Should().BeNull();
        await wrapper.Execute(context);
        JobCorrelationContext.GetCorrelationId().Should().BeNull();
    }

    [Fact]
    public async Task Execute_CorrelationAlreadySet_LogsWarning()
    {
        JobCorrelationContext.TrySetCorrelationId().Should().BeTrue();

        var jobAction = (IServiceProvider _, CancellationToken _) => Task.CompletedTask;

        var jobDetail = new StubJobDetail
        {
            JobDataMap = new JobDataMap { { QuartzJobWrapper.JobActionKey, jobAction } }
        };

        var context = new StubJobExecutionContext
        {
            Scheduler = new StubScheduler(),
            JobDetail = jobDetail,
            Trigger = new StubTrigger(),
            CancellationToken = CancellationToken.None
        };

        var wrapper = new QuartzJobWrapper(_serviceProvider, _logger);

        _fakeLogger.Clear();
        await wrapper.Execute(context);

        _fakeLogger.Entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Warning &&
            e.Message.Contains("Correlation ID already set"));
    }

    [Fact]
    public async Task Execute_DoesNotClearForeignCorrelationId()
    {
        JobCorrelationContext.TrySetCorrelationId().Should().BeTrue();
        var foreignId = JobCorrelationContext.GetCorrelationId();

        var jobAction = (IServiceProvider _, CancellationToken _) => Task.CompletedTask;

        var jobDetail = new StubJobDetail
        {
            JobDataMap = new JobDataMap { { QuartzJobWrapper.JobActionKey, jobAction } }
        };

        var context = new StubJobExecutionContext
        {
            Scheduler = new StubScheduler(),
            JobDetail = jobDetail,
            Trigger = new StubTrigger(),
            CancellationToken = CancellationToken.None
        };

        var wrapper = new QuartzJobWrapper(_serviceProvider, _logger);

        await wrapper.Execute(context);

        JobCorrelationContext.GetCorrelationId().Should().Be(foreignId);
    }

    [Fact]
    public async Task Execute_DelegateMissing_ThrowsJobExecutionException()
    {
        var jobDetail = new StubJobDetail
        {
            JobDataMap = new JobDataMap()
        };

        var context = new StubJobExecutionContext
        {
            Scheduler = new StubScheduler(),
            JobDetail = jobDetail,
            Trigger = new StubTrigger(),
            CancellationToken = CancellationToken.None
        };

        var wrapper = new QuartzJobWrapper(_serviceProvider, _logger);

        var act = () => wrapper.Execute(context);

        (await act.Should().ThrowAsync<JobExecutionException>()).Which
            .InnerException.Should().BeOfType<NullReferenceException>();
    }

    [Fact]
    public async Task Execute_CancellationRequested_ThrowsJobExecutionExceptionWithInnerCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var jobDetail = new StubJobDetail
        {
            JobDataMap = new JobDataMap
            {
                {
                    QuartzJobWrapper.JobActionKey,
                    (IServiceProvider _, CancellationToken ct) =>
                    {
                        ct.ThrowIfCancellationRequested();
                        return Task.CompletedTask;
                    }
                }
            }
        };

        var context = new StubJobExecutionContext
        {
            Scheduler = new StubScheduler(),
            JobDetail = jobDetail,
            Trigger = new StubTrigger(),
            CancellationToken = cts.Token
        };

        var wrapper = new QuartzJobWrapper(_serviceProvider, _logger);

        var act = () => wrapper.Execute(context);

        (await act.Should().ThrowAsync<JobExecutionException>()).Which
            .InnerException.Should().BeOfType<OperationCanceledException>();
    }

    [Fact]
    public async Task ProcessAsync_DefaultImplementation_DelegatesToJobDataMap()
    {
        var invoked = false;
        Func<IServiceProvider, CancellationToken, Task> jobAction = (_, _) =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        var jobDetail = new StubJobDetail
        {
            JobDataMap = new JobDataMap { { QuartzJobWrapper.JobActionKey, jobAction } }
        };

        var context = new StubJobExecutionContext
        {
            Scheduler = new StubScheduler(),
            JobDetail = jobDetail,
            Trigger = new StubTrigger(),
            CancellationToken = CancellationToken.None
        };

        var testableWrapper = new TestableQuartzJobWrapper(_serviceProvider, _logger);

        await testableWrapper.InvokeProcessAsync(context, CancellationToken.None);

        invoked.Should().BeTrue();
    }

    private sealed class TestableQuartzJobWrapper : QuartzJobWrapper
    {
        public TestableQuartzJobWrapper(IServiceProvider serviceProvider, ILogger<QuartzJobWrapper> logger)
            : base(serviceProvider, logger)
        {
        }

        public Task InvokeProcessAsync(IJobExecutionContext context, CancellationToken ct)
            => ProcessAsync(context, ct);
    }
}
