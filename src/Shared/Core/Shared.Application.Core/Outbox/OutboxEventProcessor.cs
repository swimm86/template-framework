// ----------------------------------------------------------------------------------------------
// <copyright file="OutboxEventProcessor.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Shared.Application.Core.Outbox.Interfaces;
using Shared.Domain.Core.Entities;

namespace Shared.Application.Core.Outbox;

/// <summary>
/// Процессор для обработки Outbox событий.
/// </summary>
public class OutboxEventProcessor
{
    private readonly IOutboxService _outboxService;
    private readonly IEnumerable<IOutboxEventHandler> _handlers;
    private readonly ILogger<OutboxEventProcessor> _logger;

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="outboxService">Сервис Outbox.</param>
    /// <param name="handlers">Обработчики событий.</param>
    /// <param name="logger">Логгер.</param>
    public OutboxEventProcessor(
        IOutboxService outboxService,
        IEnumerable<IOutboxEventHandler> handlers,
        ILogger<OutboxEventProcessor> logger)
    {
        _outboxService = outboxService;
        _handlers = handlers;
        _logger = logger;
    }

    /// <summary>
    /// Обрабатывает батч событий из Outbox.
    /// </summary>
    /// <param name="batchSize">Размер батча.</param>
    /// <param name="lockDurationMinutes">Длительность блокировки в минутах.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Количество обработанных событий.</returns>
    public async Task<int> ProcessBatchAsync(
        int batchSize = 100,
        int lockDurationMinutes = 5,
        CancellationToken cancellationToken = default)
    {
        var lockId = Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation("Starting outbox event processing with LockId={LockId}", lockId);

            // Получаем события для обработки
            var events = await _outboxService.GetPendingEventsAsync(
                batchSize,
                lockId,
                lockDurationMinutes,
                cancellationToken);

            if (!events.Any())
            {
                _logger.LogDebug("No pending outbox events found");
                return 0;
            }

            _logger.LogInformation("Processing {Count} outbox events", events.Count);

            var processedCount = 0;

            foreach (var outboxEvent in events)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Outbox event processing cancelled");
                    break;
                }

                await ProcessEventAsync(outboxEvent, cancellationToken);
                processedCount++;
            }

            _logger.LogInformation(
                "Completed outbox event processing: {ProcessedCount}/{TotalCount} events processed",
                processedCount,
                events.Count);

            return processedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during outbox event batch processing");
            throw;
        }
    }

    /// <summary>
    /// Обрабатывает одно событие.
    /// </summary>
    /// <param name="outboxEvent">Событие для обработки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    private async Task ProcessEventAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Processing outbox event: Id={EventId}, Type={EventType}, Attempt={Attempt}",
                outboxEvent.Id,
                outboxEvent.EventType,
                outboxEvent.RetryCount + 1);

            // Находим подходящий обработчик
            var handler = _handlers.FirstOrDefault(h => h.CanHandle(outboxEvent.EventType));

            if (handler == null)
            {
                var errorMessage = $"No handler found for event type: {outboxEvent.EventType}";
                _logger.LogWarning(errorMessage);
                await _outboxService.MarkAsFailedAsync(outboxEvent.Id, errorMessage, null, cancellationToken);
                return;
            }

            // Обрабатываем событие
            await handler.HandleAsync(outboxEvent, cancellationToken);

            // Помечаем как успешно обработанное
            await _outboxService.MarkAsProcessedAsync(outboxEvent.Id, cancellationToken);

            _logger.LogInformation(
                "Successfully processed outbox event: Id={EventId}, Type={EventType}",
                outboxEvent.Id,
                outboxEvent.EventType);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error processing outbox event: {ex.Message}";
            _logger.LogError(
                ex,
                "Failed to process outbox event: Id={EventId}, Type={EventType}",
                outboxEvent.Id,
                outboxEvent.EventType);

            await _outboxService.MarkAsFailedAsync(outboxEvent.Id, errorMessage, null, cancellationToken);
        }
    }
}

