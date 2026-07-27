// ----------------------------------------------------------------------------------------------
// <copyright file="JobExecutionIntegrationTestBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Job.Extensions;
using Shared.Application.Core.Job.Interfaces;
using Shared.Application.Core.Job.Scheduler;
using Shared.Testing.DependencyInjection;
using Xunit;

namespace Shared.Testing.Job;

/// <summary>
/// Базовый класс для интеграционных тестов адаптеров Job Scheduler (Quartz, Hangfire).
/// <para>
/// Гарантирует, что оба адаптера проходят <b>идентичный</b> набор end-to-end проверок
/// — в полном соответствии с принципом
/// <a href="https://example.com/job-scheduler/zero-touch-proof">Zero-Touch Proof</a>:
/// «смена адаптера Quartz ↔ Hangfire не должна требовать правок в тестах».
/// </para>
/// <para>
/// Тест <see cref="Bootstrapper_StartAsync_RunsOnStartupJob"/> поднимает настоящий
/// планировщик через <see cref="IHost"/> (а не моки) и проверяет, что
/// <see cref="IScheduledJob.ExecuteAsync"/> действительно вызывается после
/// старта bootstrapper-а. Это единственный тип теста, который может поймать
/// регрессию вида «адаптер не подключён к DI и джоба не выполняется в принципе»
/// (см. Quartz-регрессию 2026-06-04: <c>AddQuartz()</c> без
/// <c>UseMicrosoftDependencyInjectionJobFactory</c>).
/// </para>
/// <para>
/// Все unit-тесты используют моки и не поднимают реальный планировщик —
/// именно поэтому первоначальный баг не был пойман. Этот базовый класс
/// закрывает указанный пробел для обоих адаптеров одновременно.
/// </para>
/// </summary>
/// <remarks>
/// Конкретный адаптер задаётся наследником через <see cref="RegisterAdapter"/>.
/// Дополнительные специфичные проверки (например, наличие <c>ISchedulerFactory</c>
/// в Quartz или <c>IRecurringJobManager</c> в Hangfire) выполняются в наследниках
/// или в их <c>XxxDependencyInjectorTests</c> — здесь проверяется только общий
/// контракт «bootstrapper поднял адаптер и джоба выполнилась».
/// </remarks>
public abstract class JobExecutionIntegrationTestBase
{
    /// <summary>
    /// Таймаут ожидания срабатывания <c>OnStartup</c> джобы.
    /// Подобран с запасом: реальный Hangfire-сервер стартует дольше Quartz.
    /// </summary>
    protected static readonly TimeSpan DefaultExecutionTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Выполняет регистрацию адаптера (Quartz или Hangfire) в коллекции сервисов.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="loggerFactory">Фабрика логгеров, которую требует <c>DependencyInjectorBase</c>.</param>
    protected abstract void RegisterAdapter(IServiceCollection services, ILoggerFactory loggerFactory);

    /// <summary>
    /// Ожидает завершения <paramref name="jobTask"/> в пределах <paramref name="timeout"/>.
    /// Если задача не завершилась — выбрасывается <see cref="TimeoutException"/>;
    /// если <paramref name="cancellationToken"/> отменён — <see cref="OperationCanceledException"/>.
    /// </summary>
    /// <param name="jobTask">Задача, отслеживающая выполнение джобы.</param>
    /// <param name="timeout">Максимальное время ожидания.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены ожидания.</param>
    /// <returns>Задача, завершённая успешным выполнением джобы.</returns>
    /// <exception cref="TimeoutException">Выбрасывается, если <paramref name="jobTask"/> не завершилась за <paramref name="timeout"/>.</exception>
    /// <exception cref="OperationCanceledException">Выбрасывается, если <paramref name="cancellationToken"/> отменён до завершения <paramref name="jobTask"/>.</exception>
    protected static async Task WaitForJobWithTimeoutAsync(Task jobTask, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var winner = await Task.WhenAny(jobTask, Task.Delay(timeout, cancellationToken));

        if (winner == jobTask)
        {
            await jobTask;
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        throw new TimeoutException(
            $"Job did not complete within {timeout.TotalSeconds:N1} sec.");
    }

    /// <summary>
    /// End-to-end: после старта bootstrapper-а <see cref="IScheduledJob.ExecuteAsync"/>
    /// реально вызывается в поднятом планировщике.
    /// <para>
    /// Поднимаем <see cref="IHost"/> (вместе со всеми <see cref="IHostedService"/>,
    /// которые адаптер успел зарегистрировать) и ждём
    /// <see cref="SignalJob.ExecuteCalled"/> через <see cref="WaitForJobWithTimeoutAsync"/>.
    /// Если джоба не выполнилась за <see cref="DefaultExecutionTimeout"/> — тест падает
    /// с осмысленным сообщением, а не зависает. При медленном CI это поведение защищает
    /// от 30-секундных блокировок: тест завершается за <see cref="DefaultExecutionTimeout"/>
    /// даже при регрессии вида «адаптер не подключён к DI».
    /// </para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Bootstrapper_StartAsync_RunsOnStartupJob()
    {
        // Arrange
        using var host = BuildHost();

        // Act
        await host.StartAsync(TestContext.Current.CancellationToken);

        try
        {
            var signalJob = host.Services.GetRequiredService<SignalJob>();

            await WaitForJobWithTimeoutAsync(
                signalJob.ExecuteCalled,
                DefaultExecutionTimeout,
                TestContext.Current.CancellationToken);
        }
        finally
        {
            await host.StopAsync();
        }
    }

    /// <summary>
    /// <see cref="WaitForJobWithTimeoutAsync"/> выбрасывает <see cref="TimeoutException"/>,
    /// если джоба не завершилась в пределах таймаута.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task JobNotExecutedWithinTimeout_ThrowsTimeoutException()
    {
        // Arrange
        var neverCompletes = new TaskCompletionSource().Task;
        using var cts = new CancellationTokenSource();

        // Act
        var act = () => WaitForJobWithTimeoutAsync(
            neverCompletes,
            TimeSpan.FromMilliseconds(100),
            cts.Token);

        // Assert
        await act.Should().ThrowAsync<TimeoutException>();
    }

    /// <summary>
    /// <see cref="WaitForJobWithTimeoutAsync"/> возвращает управление без исключения,
    /// если джоба завершилась до истечения таймаута.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task JobExecutedWithinTimeout_CompletesNormally()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        await WaitForJobWithTimeoutAsync(
            Task.CompletedTask,
            TimeSpan.FromSeconds(1),
            cts.Token);

        // Assert — отсутствие исключения подтверждает успешное выполнение.
    }

    /// <summary>
    /// Несколько параллельных вызовов <see cref="WaitForJobWithTimeoutAsync"/>
    /// с независимыми задачами завершаются без исключений и не делят между собой
    /// разделяемого состояния.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ConcurrentJobs_AllCompleteWithinTimeout()
    {
        // Arrange
        var job1 = Task.CompletedTask;
        var job2 = Task.CompletedTask;
        var job3 = Task.CompletedTask;
        using var cts = new CancellationTokenSource();

        // Act
        await Task.WhenAll(
            WaitForJobWithTimeoutAsync(job1, TimeSpan.FromSeconds(1), cts.Token),
            WaitForJobWithTimeoutAsync(job2, TimeSpan.FromSeconds(1), cts.Token),
            WaitForJobWithTimeoutAsync(job3, TimeSpan.FromSeconds(1), cts.Token));

        // Assert — отсутствие исключения подтверждает успешное параллельное выполнение.
    }

    /// <summary>
    /// <see cref="WaitForJobWithTimeoutAsync"/> не блокирует поток дольше таймаута:
    /// после срабатывания таймаута тест завершается за адекватное время,
    /// а не «зависает» до конца CI-runner-а.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task TimeoutTriggered_DoesNotHangForever()
    {
        // Arrange
        var neverCompletes = new TaskCompletionSource().Task;
        using var cts = new CancellationTokenSource();
        var stopwatch = Stopwatch.StartNew();
        var configuredTimeout = TimeSpan.FromMilliseconds(100);
        const double toleranceMultiplier = 2;

        // Act
        var act = () => WaitForJobWithTimeoutAsync(
            neverCompletes,
            configuredTimeout,
            cts.Token);

        await act.Should().ThrowAsync<TimeoutException>();

        // Assert
        stopwatch.Stop();
        stopwatch.Elapsed.Should().BeLessThan(
            configuredTimeout * toleranceMultiplier,
            "хелпер должен пробудиться почти сразу по таймауту, а не висеть дальше");
    }

    private IHost BuildHost() =>
        Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddSingleton<SignalJob>();
                services.AddJobs(opts => opts.AddJob<SignalJob>(new JobSchedule.OnStartup()));
                RegisterAdapter(services, LoggerFactory.Create(b => { }));
            })
            .Build();
}
