using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Shared.Application.Core.CorrelationId;
using Shared.Infrastructure.Job.Quartz.Tests.Infrastructure;
using Shared.Testing.Doubles.Logging;

namespace Shared.Infrastructure.Job.Quartz.Tests;

/// <summary>
/// Модульные тесты для QuartzJobWrapper.
/// </summary>
public sealed class QuartzJobWrapperTests
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

    /// <summary>
    /// Успешное выполнение вызывает делегат из JobDataMap.
    /// </summary>
    [Fact]
    public async Task Execute_HappyPath_InvokesDelegateFromJobDataMap()
    {
        // Arrange
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

        // Act
        await wrapper.Execute(context);

        // Assert
        invoked.Should().BeTrue();
    }

    /// <summary>
    /// Успешное выполнение не планирует повторную попытку.
    /// </summary>
    [Fact]
    public async Task Execute_HappyPath_DoesNotScheduleRetry()
    {
        // Arrange
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

        // Act
        await wrapper.Execute(context);

        // Assert
        scheduler.ScheduleJobCalled.Should().BeFalse();
    }

    /// <summary>
    /// Исключение в делегате планирует повтор и выбрасывает JobExecutionException.
    /// </summary>
    [Fact]
    public async Task Execute_DelegateThrows_SchedulesRetryAndThrowsJobExecutionException()
    {
        // Arrange
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

        // Act & Assert
        var act = () => wrapper.Execute(context);

        var ex = (await act.Should().ThrowAsync<JobExecutionException>()).Which;
        ex.InnerException.Should().Be(exception);
        ex.RefireImmediately.Should().BeFalse();

        scheduler.ScheduleJobCalled.Should().BeTrue();
    }

    /// <summary>
    /// CancellationToken передаётся без изменений.
    /// </summary>
    [Fact]
    public async Task Execute_PreservesCancellationToken()
    {
        // Arrange
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

        // Act
        await wrapper.Execute(context);

        // Assert
        passedToken.Should().Be(cts.Token);
    }

    /// <summary>
    /// Устанавливает и очищает Correlation ID.
    /// </summary>
    [Fact]
    public async Task Execute_SetsAndClearsCorrelationId()
    {
        // Arrange
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

        // Act
        await wrapper.Execute(context);

        // Assert
        JobCorrelationContext.GetCorrelationId().Should().BeNull();
    }

    /// <summary>
    /// Если Correlation ID уже установлен — логирует предупреждение.
    /// </summary>
    [Fact]
    public async Task Execute_CorrelationAlreadySet_LogsWarning()
    {
        // Arrange
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

        // Act
        await wrapper.Execute(context);

        // Assert
        _fakeLogger.Entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Warning &&
            e.Message.Contains("Correlation ID already set"));
    }

    /// <summary>
    /// Не очищает чужой Correlation ID.
    /// </summary>
    [Fact]
    public async Task Execute_DoesNotClearForeignCorrelationId()
    {
        // Arrange
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

        // Act
        await wrapper.Execute(context);

        // Assert
        JobCorrelationContext.GetCorrelationId().Should().Be(foreignId);
    }

    /// <summary>
    /// Отсутствие делегата в JobDataMap вызывает JobExecutionException.
    /// </summary>
    [Fact]
    public async Task Execute_DelegateMissing_ThrowsJobExecutionException()
    {
        // Arrange
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

        // Act & Assert
        var act = () => wrapper.Execute(context);

        (await act.Should().ThrowAsync<JobExecutionException>()).Which
            .InnerException.Should().BeOfType<NullReferenceException>();
    }

    /// <summary>
    /// Отмена операции вызывает JobExecutionException с внутренним OperationCanceledException.
    /// </summary>
    [Fact]
    public async Task Execute_CancellationRequested_ThrowsJobExecutionExceptionWithInnerCancellation()
    {
        // Arrange
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

        // Act & Assert
        var act = () => wrapper.Execute(context);

        (await act.Should().ThrowAsync<JobExecutionException>()).Which
            .InnerException.Should().BeOfType<OperationCanceledException>();
    }

    /// <summary>
    /// ProcessAsync по умолчанию делегирует вызов JobDataMap.
    /// </summary>
    [Fact]
    public async Task ProcessAsync_DefaultImplementation_DelegatesToJobDataMap()
    {
        // Arrange
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

        // Act
        await testableWrapper.InvokeProcessAsync(context, CancellationToken.None);

        // Assert
        invoked.Should().BeTrue();
    }

    /// <summary>
    /// Тестируемая обёртка QuartzJobWrapper, предоставляющая доступ к protected методу ProcessAsync.
    /// </summary>
    private sealed class TestableQuartzJobWrapper(
        IServiceProvider serviceProvider,
        ILogger<QuartzJobWrapper> logger)
        : QuartzJobWrapper(serviceProvider, logger)
    {
        public Task InvokeProcessAsync(IJobExecutionContext context, CancellationToken ct)
            => ProcessAsync(context, ct);
    }
}
