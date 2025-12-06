// ----------------------------------------------------------------------------------------------
// <copyright file="OutboxProcessorJob.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Shared.Application.Core.Outbox;

namespace Shared.Infrastructure.Job.Quartz.Outbox;

/// <summary>
/// Задача для обработки Outbox событий в фоновом режиме.
/// </summary>
[DisallowConcurrentExecution]
public class OutboxProcessorJob : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorJob> _logger;

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="serviceProvider">Service provider.</param>
    /// <param name="logger">Логгер.</param>
    public OutboxProcessorJob(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Starting outbox processor job");

            using var scope = _serviceProvider.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<OutboxEventProcessor>();

            // Получаем параметры из JobDataMap или используем значения по умолчанию
            var batchSize = context.JobDetail.JobDataMap.GetIntValue("BatchSize");
            if (batchSize == 0)
            {
                batchSize = 100;
            }

            var lockDurationMinutes = context.JobDetail.JobDataMap.GetIntValue("LockDurationMinutes");
            if (lockDurationMinutes == 0)
            {
                lockDurationMinutes = 5;
            }

            var processedCount = await processor.ProcessBatchAsync(
                batchSize,
                lockDurationMinutes,
                context.CancellationToken);

            _logger.LogInformation(
                "Outbox processor job completed: {ProcessedCount} events processed",
                processedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing outbox processor job");
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }
}

