// ----------------------------------------------------------------------------------------------
// <copyright file="HangfireJobSchedulerBootstrapperTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Application.Core.Job.Interfaces;
using Shared.Application.Core.Job.Scheduler;
using Shared.Application.Core.Job.Scheduler.Interfaces;

namespace Shared.Infrastructure.Job.Hangfire.Tests;

/// <summary>
/// Тесты <see cref="HangfireJobSchedulerBootstrapper"/>: проверяем, что bootstrapper
/// читает <see cref="JobSchedulerOptions"/> и регистрирует каждое определение через
/// <see cref="IJobScheduler.ScheduleAsync"/>.
/// </summary>
public sealed class HangfireJobSchedulerBootstrapperTests
{
    /// <summary>
    /// Пустой список <see cref="JobSchedulerOptions.Definitions"/> приводит к тому,
    /// что <see cref="IJobScheduler.ScheduleAsync"/> не вызывается ни разу.
    /// </summary>
    [Fact]
    public async Task DoesNotCallScheduler_WhenDefinitionsAreEmpty()
    {
        // Arrange
        var scheduler = new Mock<IJobScheduler>();
        var bootstrapper = new HangfireJobSchedulerBootstrapper(
            new JobSchedulerOptions(),
            scheduler.Object,
            NullLogger<HangfireJobSchedulerBootstrapper>.Instance);

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
    public async Task SchedulesEachDefinition_WhenMultipleJobsProvided()
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

        var bootstrapper = new HangfireJobSchedulerBootstrapper(
            options,
            scheduler.Object,
            NullLogger<HangfireJobSchedulerBootstrapper>.Instance);

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
    public async Task DoesNotThrow_OnStartStopRoundTrip()
    {
        // Arrange
        var scheduler = new Mock<IJobScheduler>();
        var bootstrapper = new HangfireJobSchedulerBootstrapper(
            new JobSchedulerOptions(),
            scheduler.Object,
            NullLogger<HangfireJobSchedulerBootstrapper>.Instance);

        // Act / Assert
        await bootstrapper.StartAsync(CancellationToken.None);
        await bootstrapper.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// <see cref="HangfireJobSchedulerBootstrapper.StartAsync"/> пробрасывает
    /// <see cref="CancellationToken"/> в <see cref="IJobScheduler.ScheduleAsync"/>.
    /// </summary>
    [Fact]
    public async Task ForwardsCancellationTokenToScheduler_WhenProvided()
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

        var bootstrapper = new HangfireJobSchedulerBootstrapper(
            options,
            scheduler.Object,
            NullLogger<HangfireJobSchedulerBootstrapper>.Instance);

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
    public async Task PropagatesSchedulerException_WhenSchedulerThrows()
    {
        // Arrange
        var definitions = new[] { NewDefinition("failing-job") };
        var options = new JobSchedulerOptions { Definitions = definitions };

        var scheduler = new Mock<IJobScheduler>();
        scheduler
            .Setup(s => s.ScheduleAsync(It.IsAny<JobDefinition>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("scheduler boom"));

        var bootstrapper = new HangfireJobSchedulerBootstrapper(
            options,
            scheduler.Object,
            NullLogger<HangfireJobSchedulerBootstrapper>.Instance);

        // Act
        var act = () => bootstrapper.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("scheduler boom");
    }

    private static JobDefinition NewDefinition(string jobKey) =>
        new(
            JobKey: jobKey,
            Action: null,
            Schedule: new JobSchedule.OnStartup(),
            JobType: typeof(FakeScheduledJob),
            ServiceKey: null);

    /// <summary>
    /// Минимальный тип джобы для <see cref="JobDefinition.JobType"/>.
    /// </summary>
    private sealed class FakeScheduledJob : IScheduledJob
    {
        public Task ExecuteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
