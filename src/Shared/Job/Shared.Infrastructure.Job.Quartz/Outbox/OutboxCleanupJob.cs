// ----------------------------------------------------------------------------------------------
// <copyright file="OutboxCleanupJob.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Shared.Application.Core.Outbox.Interfaces;

namespace Shared.Infrastructure.Job.Quartz.Outbox;

/// <summary>
/// Задача для очистки устаревших обработанных событий и снятия просроченных блокировок.
/// </summary>
[DisallowConcurrentExecution]
public class OutboxCleanupJob : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxCleanupJob> _logger;

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="serviceProvider">Service provider.</param>
    /// <param name="logger">Логгер.</param>
    public OutboxCleanupJob(
        IServiceProvider serviceProvider,
        ILogger<OutboxCleanupJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Starting outbox cleanup job");

            using var scope = _serviceProvider.CreateScope();
            var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

            // Снимаем просроченные блокировки
            var releasedLocksCount = await outboxService.ReleaseExpiredLocksAsync(context.CancellationToken);
            
            _logger.LogInformation(
                "Released {Count} expired locks",
                releasedLocksCount);

            // Очищаем устаревшие обработанные события
            var olderThanDays = context.JobDetail.JobDataMap.GetIntValue("OlderThanDays");
            if (olderThanDays == 0)
            {
                olderThanDays = 30;
            }

            var deletedCount = await outboxService.CleanupProcessedEventsAsync(
                olderThanDays,
                context.CancellationToken);

            _logger.LogInformation(
                "Outbox cleanup job completed: {DeletedCount} events deleted, {ReleasedCount} locks released",
                deletedCount,
                releasedLocksCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing outbox cleanup job");
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }
}

