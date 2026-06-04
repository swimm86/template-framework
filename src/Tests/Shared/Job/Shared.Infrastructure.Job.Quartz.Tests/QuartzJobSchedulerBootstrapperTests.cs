// ----------------------------------------------------------------------------------------------
// <copyright file="QuartzJobSchedulerBootstrapperTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;
using Shared.Application.Core.Job.Scheduler;
using Shared.Application.Core.Job.Scheduler.Interfaces;
using Shared.Infrastructure.Job.Quartz.Tests.Fakes;
using Shared.Testing.Doubles.Logging;
using Shared.Testing.Job;

namespace Shared.Infrastructure.Job.Quartz.Tests;

/// <summary>
/// Тесты <see cref="QuartzJobSchedulerBootstrapper"/>: проверяем, что bootstrapper
/// читает <see cref="JobSchedulerOptions"/>, регистрирует каждое определение через
/// <see cref="IJobScheduler.ScheduleAsync"/> и стартует/останавливает Quartz-планировщик.
/// <para>
/// Структура тестов полностью симметрична <c>HangfireJobSchedulerBootstrapperTests</c>
/// (общий контракт: <c>StartAsync_EmptyDefinitions_DoesNotCallScheduler</c>,
/// <c>StartAsync_ThreeJobs_CallsSchedulerOncePerDefinition</c>,
/// <c>StartStopAsync_RoundTrip_DoesNotThrow</c>,
/// <c>StartAsync_ForwardsCancellationToken</c>,
/// <c>StartAsync_SchedulerThrows_PropagatesException</c>,
/// <c>StartAsync_ExceptionOnSecondJob_StopsSchedulingRemaining</c>) —
/// иначе нельзя гарантировать Zero-Touch Proof (смена Quartz ↔ Hangfire = 0 правок).
/// </para>
/// <para>
/// <see cref="QuartzJobSchedulerBootstrapper"/> — единственный владелец жизненного
/// цикла <see cref="IScheduler"/>, потому что <c>AddQuartz</c> в
/// <see cref="QuartzDependencyInjector"/> <c>IHostedService</c> не регистрирует
/// (для этого нужен отдельный <c>AddQuartzHostedService</c>). Это отличает его от
/// Hangfire-аналога (где воркер стартует через <c>JobServerHostedService</c>) и
/// объясняет наличие <c>StartAsync_*_StartsQuartz</c> /
/// <c>StopAsync_InvokesSchedulerShutdownWithWaitForJobs</c> в Quartz-варианте.
/// </para>
/// </summary>
public sealed class QuartzJobSchedulerBootstrapperTests
{
    /// <summary>
    /// Пустой список <see cref="JobSchedulerOptions.Definitions"/> приводит к тому,
    /// что <see cref="IJobScheduler.ScheduleAsync"/> не вызывается ни разу, и
    /// планировщик всё равно стартует и останавливается без ошибок.
    /// </summary>
    [Fact]
    public async Task StartAsync_EmptyDefinitions_DoesNotCallScheduler()
    {
        // Arrange
        var scheduler = new Mock<IJobScheduler>();
        var factory = new FakeSchedulerFactory();
        var bootstrapper = NewBootstrapper(new JobSchedulerOptions(), scheduler.Object, factory);

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

        var factory = new FakeSchedulerFactory();
        var bootstrapper = NewBootstrapper(options, scheduler.Object, factory);

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
    /// <see cref="QuartzJobSchedulerBootstrapper.StartAsync"/> и
    /// <see cref="QuartzJobSchedulerBootstrapper.StopAsync"/> выполняются без
    /// исключений при пустых определениях.
    /// </summary>
    [Fact]
    public async Task StartStopAsync_RoundTrip_DoesNotThrow()
    {
        // Arrange
        var scheduler = new Mock<IJobScheduler>();
        var factory = new FakeSchedulerFactory();
        var bootstrapper = NewBootstrapper(new JobSchedulerOptions(), scheduler.Object, factory);

        // Act / Assert
        await bootstrapper.StartAsync(CancellationToken.None);
        await bootstrapper.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// <see cref="QuartzJobSchedulerBootstrapper.StartAsync"/> пробрасывает
    /// <see cref="CancellationToken"/> в <see cref="IJobScheduler.ScheduleAsync"/>,
    /// в <see cref="ISchedulerFactory.GetScheduler(CancellationToken)"/> и в
    /// <see cref="IScheduler.Start"/>.
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

        var factory = new FakeSchedulerFactory();
        var bootstrapper = NewBootstrapper(options, scheduler.Object, factory);

        // Act
        await bootstrapper.StartAsync(expectedToken);

        // Assert
        scheduler.Verify(
            s => s.ScheduleAsync(It.IsAny<JobDefinition>(), expectedToken),
            Times.Once);
        factory.GetSchedulerCalls.Should().Contain(expectedToken);
    }

    /// <summary>
    /// <see cref="QuartzJobSchedulerBootstrapper.StopAsync"/> вызывает
    /// <see cref="IScheduler.Shutdown(bool, CancellationToken)"/> с
    /// <c>waitForJobsToComplete = true</c> и пробрасывает <see cref="CancellationToken"/>.
    /// <para>
    /// Специфично для Quartz: Hangfire-аналог делегирует остановку
    /// <c>JobServerHostedService</c> и не вызывает <c>Shutdown</c> явно.
    /// </para>
    /// </summary>
    [Fact]
    public async Task StopAsync_InvokesSchedulerShutdownWithWaitForJobs()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var expectedToken = cts.Token;

        var scheduler = new Mock<IJobScheduler>();
        var factory = new FakeSchedulerFactory();
        var bootstrapper = NewBootstrapper(new JobSchedulerOptions(), scheduler.Object, factory);

        // Act
        await bootstrapper.StopAsync(expectedToken);

        // Assert
        factory.SchedulerMock.Verify(
            s => s.Shutdown(true, expectedToken),
            Times.Once);
    }

    /// <summary>
    /// Исключение из <see cref="IJobScheduler.ScheduleAsync"/> пробрасывается
    /// вызывающему коду <see cref="QuartzJobSchedulerBootstrapper.StartAsync"/>.
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

        var factory = new FakeSchedulerFactory();
        var bootstrapper = NewBootstrapper(options, scheduler.Object, factory);

        // Act
        var act = () => bootstrapper.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("scheduler boom");
    }

    /// <summary>
    /// Если <see cref="IJobScheduler.ScheduleAsync"/> для второй джобы кидает исключение,
    /// то третья не планируется и Quartz не стартует. Проверяем, что цикл по
    /// определениям не проглатывает ошибки и сразу выходит из <c>StartAsync</c>,
    /// а <see cref="IScheduler.Start"/> не вызывается (атомарный контракт:
    /// «если регистрация упала, ничего не запустилось»).
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

        var factory = new FakeSchedulerFactory();
        var bootstrapper = NewBootstrapper(options, scheduler.Object, factory);

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
        factory.SchedulerMock.Verify(
            s => s.Start(It.IsAny<CancellationToken>()),
            Times.Never,
            "Quartz-планировщик не должен стартовать, если хотя бы одна джоба не зарегистрировалась");
    }

    /// <summary>
    /// <see cref="QuartzJobSchedulerBootstrapper"/> логирует количество регистрируемых
    /// джоб в Information-уровне.
    /// <para>
    /// Специфично для Quartz (проверяется <see cref="QuartzJobScheduler"/>): Hangfire
    /// логирует аналогично, но семантика уровней логирования проверяется в
    /// <c>HangfireJobSchedulerBootstrapperTests</c> независимо, поскольку у этих
    /// классов разные логгеры.
    /// </para>
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

        var factory = new FakeSchedulerFactory();
        var bootstrapper = new QuartzJobSchedulerBootstrapper(
            options,
            scheduler.Object,
            factory,
            new FakeLogger<QuartzJobSchedulerBootstrapper>(logger));

        // Act
        await bootstrapper.StartAsync(CancellationToken.None);

        // Assert
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Information && e.Message.Contains("2"));
    }

    private static QuartzJobSchedulerBootstrapper NewBootstrapper(
        JobSchedulerOptions options,
        IJobScheduler scheduler,
        ISchedulerFactory factory) =>
        new(options, scheduler, factory, NullLogger<QuartzJobSchedulerBootstrapper>.Instance);

    private static JobDefinition NewDefinition(string jobKey) =>
        new(
            JobKey: jobKey,
            Action: null,
            Schedule: new JobSchedule.OnStartup(),
            JobType: typeof(FakeScheduledJob),
            ServiceKey: null);
}
