// ----------------------------------------------------------------------------------------------
// <copyright file="JobExecutionIntegrationTestBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

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
    protected static readonly TimeSpan DefaultExecutionTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Выполняет регистрацию адаптера (Quartz или Hangfire) в коллекции сервисов.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="loggerFactory">Фабрика логгеров, которую требует <c>DependencyInjectorBase</c>.</param>
    protected abstract void RegisterAdapter(IServiceCollection services, ILoggerFactory loggerFactory);

    /// <summary>
    /// End-to-end: после старта bootstrapper-а <see cref="IScheduledJob.ExecuteAsync"/>
    /// реально вызывается в поднятом планировщике.
    /// <para>
    /// Поднимаем <see cref="IHost"/> (вместе со всеми <see cref="IHostedService"/>,
    /// которые адаптер успел зарегистрировать) и ждём
    /// <see cref="SignalJob.ExecuteCalled"/>. Если <c>Task.WhenAny</c> вернёт
    /// задачу-таймаут — тест падает с осмысленным сообщением через
    /// <see cref="DefaultExecutionTimeout"/>, а не зависает.
    /// </para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Bootstrapper_StartAsync_RunsOnStartupJob()
    {
        // Arrange
        using var host = BuildHost();

        // Act
        await host.StartAsync(CancellationToken.None);

        try
        {
            var signalJob = host.Services.GetRequiredService<SignalJob>();
            var completed = await Task.WhenAny(
                signalJob.ExecuteCalled,
                Task.Delay(DefaultExecutionTimeout));

            // Assert
            completed
                .Should()
                .BeSameAs(
                    signalJob.ExecuteCalled,
                    "OnStartup job должен сработать после старта bootstrapper-а; " +
                    $"если тест упал по таймауту {DefaultExecutionTimeout.TotalSeconds:N0}s — " +
                    "вероятно, адаптер не подключён к DI (см. регрессию 2026-06-04)");
        }
        finally
        {
            await host.StopAsync();
        }
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
