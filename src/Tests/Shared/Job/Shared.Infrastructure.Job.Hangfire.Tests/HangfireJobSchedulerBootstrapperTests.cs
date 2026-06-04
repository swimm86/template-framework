// ----------------------------------------------------------------------------------------------
// <copyright file="HangfireJobSchedulerBootstrapperTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Application.Core.Job.Scheduler;
using Shared.Application.Core.Job.Scheduler.Interfaces;
using Shared.Testing.Doubles.Logging;
using Shared.Testing.Job;

namespace Shared.Infrastructure.Job.Hangfire.Tests;

/// <summary>
/// Тесты <see cref="HangfireJobSchedulerBootstrapper"/>: проверяем, что bootstrapper
/// читает <see cref="JobSchedulerOptions"/> и регистрирует каждое определение через
/// <see cref="IJobScheduler.ScheduleAsync"/>.
/// <para>
/// Структура тестов полностью симметрична <c>QuartzJobSchedulerBootstrapperTests</c>
/// (общий контракт: <c>StartAsync_EmptyDefinitions_DoesNotCallScheduler</c>,
/// <c>StartAsync_ThreeJobs_CallsSchedulerOncePerDefinition</c>,
/// <c>StartStopAsync_RoundTrip_DoesNotThrow</c>,
/// <c>StartAsync_ForwardsCancellationToken</c>,
/// <c>StartAsync_SchedulerThrows_PropagatesException</c>,
/// <c>StartAsync_ExceptionOnSecondJob_StopsSchedulingRemaining</c>,
/// <c>StartAsync_LogsInformationWithJobCount</c>) — иначе нельзя гарантировать
/// Zero-Touch Proof (смена Quartz ↔ Hangfire = 0 правок).
/// </para>
/// <para>
/// Hangfire-сервер стартует автоматически через <c>JobServerHostedService</c>,
/// поэтому <see cref="HangfireJobSchedulerBootstrapper.StartAsync"/> не вызывает
/// <c>scheduler.Start</c> явно, а <see cref="HangfireJobSchedulerBootstrapper.StopAsync"/>
/// возвращает <see cref="Task.CompletedTask"/> без <c>Shutdown</c>. Этим объясняется
/// отсутствие <c>StopAsync_InvokesSchedulerShutdownWithWaitForJobs</c> в Hangfire-варианте.
/// </para>
/// </summary>
public sealed class HangfireJobSchedulerBootstrapperTests
{
    /// <summary>
    /// Пустой список <see cref="JobSchedulerOptions.Definitions"/> приводит к тому,
    /// что <see cref="IJobScheduler.ScheduleAsync"/> не вызывается ни разу.
    /// </summary>
    [Fact]
    public async Task StartAsync_EmptyDefinitions_DoesNotCallScheduler()
    {
        // Arrange
        var scheduler = new Mock<IJobScheduler>();
        var bootstrapper = NewBootstrapper(new JobSchedulerOptions(), scheduler.Object);

        // Act
        await bootstrapper.StartAsync(CancellationToken.None);

        // Assert
        scheduler.Verify(
            s => s.ScheduleAsync(It.IsAny<JobDefinition>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Три определения в <see cref="JobSchedulerOptions.Definitions"/> приводят
    /// к трём вызовам <see cref="IJobScheduler.ScheduleAsync"/>.
    /// </summary>
    [Fact]
    public async Task StartAsync_ThreeJobs_CallsSchedulerOncePerDefinition()
    {
        // Arrange
        var definitions = new[]
        {
            NewDefinition("job-1"),
            NewDefinition("job-2"),
            NewDefinition("job-3"),
        };
        var options = new JobSchedulerOptions { Definitions = definitions };

        var scheduler = new Mock<IJobScheduler>();
        scheduler
            .Setup(s => s.ScheduleAsync(It.IsAny<JobDefinition>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var bootstrapper = NewBootstrapper(options, scheduler.Object);

        // Act
        await bootstrapper.StartAsync(CancellationToken.None);

        // Assert
        foreach (var def in definitions)
        {
            scheduler.Verify(
                s => s.ScheduleAsync(
                    It.Is<JobDefinition>(d => d.JobKey == def.JobKey),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    /// <summary>
    /// <see cref="HangfireJobSchedulerBootstrapper.StartAsync"/> и
    /// <see cref="HangfireJobSchedulerBootstrapper.StopAsync"/> выполняются без
    /// исключений при пустых определениях.
    /// </summary>
    [Fact]
    public async Task StartStopAsync_RoundTrip_DoesNotThrow()
    {
        // Arrange
        var scheduler = new Mock<IJobScheduler>();
        var bootstrapper = NewBootstrapper(new JobSchedulerOptions(), scheduler.Object);

        // Act / Assert
        await bootstrapper.StartAsync(CancellationToken.None);
        await bootstrapper.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// <see cref="HangfireJobSchedulerBootstrapper.StartAsync"/> пробрасывает
    /// <see cref="CancellationToken"/> в <see cref="IJobScheduler.ScheduleAsync"/>.
    /// </summary>
    [Fact]
    public async Task StartAsync_ForwardsCancellationToken()
    {
        // Arrange
        var definitions = new[] { NewDefinition("job-1") };
        var options = new JobSchedulerOptions { Definitions = definitions };

        using var cts = new CancellationTokenSource();
        var expectedToken = cts.Token;

        var scheduler = new Mock<IJobScheduler>();
        scheduler
            .Setup(s => s.ScheduleAsync(It.IsAny<JobDefinition>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var bootstrapper = NewBootstrapper(options, scheduler.Object);

        // Act
        await bootstrapper.StartAsync(expectedToken);

        // Assert
        scheduler.Verify(
            s => s.ScheduleAsync(It.IsAny<JobDefinition>(), expectedToken),
            Times.Once);
    }

    /// <summary>
    /// Исключение из <see cref="IJobScheduler.ScheduleAsync"/> пробрасывается
    /// вызывающему коду <see cref="HangfireJobSchedulerBootstrapper.StartAsync"/>.
    /// </summary>
    [Fact]
    public async Task StartAsync_SchedulerThrows_PropagatesException()
    {
        // Arrange
        var definitions = new[] { NewDefinition("failing-job") };
        var options = new JobSchedulerOptions { Definitions = definitions };

        var scheduler = new Mock<IJobScheduler>();
        scheduler
            .Setup(s => s.ScheduleAsync(It.IsAny<JobDefinition>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("scheduler boom"));

        var bootstrapper = NewBootstrapper(options, scheduler.Object);

        // Act
        var act = () => bootstrapper.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("scheduler boom");
    }

    /// <summary>
    /// Если <see cref="IJobScheduler.ScheduleAsync"/> для второй джобы кидает исключение,
    /// то третья не планируется. Проверяем, что цикл по определениям не проглатывает
    /// ошибки и сразу выходит из <c>StartAsync</c>.
    /// </summary>
    [Fact]
    public async Task StartAsync_ExceptionOnSecondJob_StopsSchedulingRemaining()
    {
        // Arrange
        var definitions = new[]
        {
            NewDefinition("ok-job"),
            NewDefinition("failing-job"),
            NewDefinition("never-job"),
        };
        var options = new JobSchedulerOptions { Definitions = definitions };

        var scheduler = new Mock<IJobScheduler>();
        scheduler
            .Setup(s => s.ScheduleAsync(
                It.Is<JobDefinition>(d => d.JobKey == "failing-job"),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        scheduler
            .Setup(s => s.ScheduleAsync(
                It.Is<JobDefinition>(d => d.JobKey != "failing-job"),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var bootstrapper = NewBootstrapper(options, scheduler.Object);

        // Act
        var act = () => bootstrapper.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        scheduler.Verify(
            s => s.ScheduleAsync(
                It.Is<JobDefinition>(d => d.JobKey == "never-job"),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "исключение на 2-й джобе должно остановить цикл — 3-я джоба не должна планироваться");
    }

    /// <summary>
    /// <see cref="HangfireJobSchedulerBootstrapper"/> логирует количество регистрируемых
    /// джоб в Information-уровне.
    /// </summary>
    [Fact]
    public async Task StartAsync_LogsInformationWithJobCount()
    {
        // Arrange
        var logger = new FakeLogger();
        var definitions = new[] { NewDefinition("a"), NewDefinition("b") };
        var options = new JobSchedulerOptions { Definitions = definitions };

        var scheduler = new Mock<IJobScheduler>();
        scheduler
            .Setup(s => s.ScheduleAsync(It.IsAny<JobDefinition>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var bootstrapper = new HangfireJobSchedulerBootstrapper(
            options,
            scheduler.Object,
            new FakeLogger<HangfireJobSchedulerBootstrapper>(logger));

        // Act
        await bootstrapper.StartAsync(CancellationToken.None);

        // Assert
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Information && e.Message.Contains("2"));
    }

    private static HangfireJobSchedulerBootstrapper NewBootstrapper(
        JobSchedulerOptions options,
        IJobScheduler scheduler) =>
        new(options, scheduler, NullLogger<HangfireJobSchedulerBootstrapper>.Instance);

    private static JobDefinition NewDefinition(string jobKey) =>
        new(
            JobKey: jobKey,
            Action: null,
            Schedule: new JobSchedule.OnStartup(),
            JobType: typeof(FakeScheduledJob),
            ServiceKey: null);
}
