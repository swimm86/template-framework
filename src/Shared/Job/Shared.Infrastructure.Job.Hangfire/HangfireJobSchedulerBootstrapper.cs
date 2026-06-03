// ----------------------------------------------------------------------------------------------
// <copyright file="HangfireJobSchedulerBootstrapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Job.Scheduler;
using Shared.Application.Core.Job.Scheduler.Interfaces;

namespace Shared.Infrastructure.Job.Hangfire;

/// <summary>
/// <see cref="IHostedService"/>, который при старте приложения регистрирует все задачи
/// из <see cref="JobSchedulerOptions"/> через <see cref="IJobScheduler"/>.
/// </summary>
/// <remarks>
/// Hangfire-сервер поднимается автоматически через <c>AddHangfireServer</c> в
/// <see cref="HangfireDependencyInjector"/>, отдельный вызов <c>Start</c> не требуется.
/// </remarks>
/// <param name="options">Список зарегистрированных задач.</param>
/// <param name="scheduler">Планировщик (Hangfire-реализация).</param>
/// <param name="logger">Логгер.</param>
public sealed class HangfireJobSchedulerBootstrapper(
    JobSchedulerOptions options,
    IJobScheduler scheduler,
    ILogger<HangfireJobSchedulerBootstrapper> logger)
    : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Registering {Count} job(s) in Hangfire scheduler.",
            options.Definitions.Count);

        foreach (var definition in options.Definitions)
        {
            await scheduler.ScheduleAsync(definition, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Hangfire-сервер останавливается через IDisposable/IHostedService, зарегистрированный
        // AddHangfireServer(). Дополнительных действий не требуется.
        return Task.CompletedTask;
    }
}
