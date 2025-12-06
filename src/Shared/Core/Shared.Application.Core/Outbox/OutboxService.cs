// ----------------------------------------------------------------------------------------------
// <copyright file="OutboxService.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Shared.Application.Core.Outbox.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Entities;
using Shared.Domain.Core.Enums;

namespace Shared.Application.Core.Outbox;

/// <summary>
/// Сервис для работы с Outbox событиями.
/// </summary>
public class OutboxService : IOutboxService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OutboxService> _logger;

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="unitOfWork">Unit of Work.</param>
    /// <param name="logger">Логгер.</param>
    public OutboxService(
        IUnitOfWork unitOfWork,
        ILogger<OutboxService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<OutboxEvent> AddAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<OutboxEvent>();
            var result = await repository.AddAsync(outboxEvent, null, null, cancellationToken);
            await _unitOfWork.SaveChangesAsync(true, cancellationToken);

            _logger.LogInformation(
                "Outbox event added: Id={EventId}, Type={EventType}, CorrelationId={CorrelationId}",
                result.Id,
                result.EventType,
                result.CorrelationId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add outbox event: Type={EventType}", outboxEvent.EventType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task AddRangeAsync(IEnumerable<OutboxEvent> outboxEvents, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<OutboxEvent>();
            await repository.AddRangeAsync(outboxEvents, null, null, cancellationToken);
            await _unitOfWork.SaveChangesAsync(true, cancellationToken);

            _logger.LogInformation("Added {Count} outbox events", outboxEvents.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add outbox events batch");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<OutboxEvent>> GetPendingEventsAsync(
        int batchSize = 100,
        string? lockId = null,
        int lockDurationMinutes = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<OutboxEvent>();
            var now = DateTime.UtcNow;

            // Получаем события, которые:
            // 1. В статусе Pending или Failed (с учетом NextAttemptAt)
            // 2. Не заблокированы или блокировка истекла
            // 3. Не превысили максимальное количество попыток
            var events = await repository.GetRangeAsync(
                options: new Domain.Core.Dal.Repository.Models.QueryOptions<OutboxEvent>(true)
                    .AddFilter(e =>
                        (e.Status == OutboxEventStatus.Pending ||
                         (e.Status == OutboxEventStatus.Failed && (e.NextAttemptAt == null || e.NextAttemptAt <= now))) &&
                        (e.LockExpiresAt == null || e.LockExpiresAt <= now) &&
                        e.RetryCount < e.MaxRetryCount)
                    .AddOrderBy(e => e.Priority, Domain.Core.Dal.OrderDirectionType.Descending)
                    .AddOrderBy(e => e.CreatedAt, Domain.Core.Dal.OrderDirectionType.Ascending),
                take: batchSize,
                cancellationToken: cancellationToken);

            // Блокируем события, если указан lockId
            if (!string.IsNullOrEmpty(lockId) && events.Any())
            {
                var lockExpiresAt = now.AddMinutes(lockDurationMinutes);
                var eventIds = events.Select(e => e.Id).ToList();

                foreach (var eventId in eventIds)
                {
                    var eventEntity = events.First(e => e.Id == eventId);
                    eventEntity.LockId = lockId;
                    eventEntity.LockExpiresAt = lockExpiresAt;
                    eventEntity.Status = OutboxEventStatus.Processing;
                }

                await _unitOfWork.SaveChangesAsync(true, cancellationToken);

                _logger.LogInformation(
                    "Locked {Count} outbox events with LockId={LockId}",
                    events.Count,
                    lockId);
            }

            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending outbox events");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task MarkAsProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<OutboxEvent>();
            var eventEntity = await repository.GetAsync(eventId, cancellationToken: cancellationToken);
            
            if (eventEntity == null)
            {
                _logger.LogWarning("Outbox event not found: Id={EventId}", eventId);
                return;
            }

            eventEntity.Status = OutboxEventStatus.Processed;
            eventEntity.ProcessedAt = DateTime.UtcNow;
            eventEntity.LockId = null;
            eventEntity.LockExpiresAt = null;
            eventEntity.ErrorMessage = null;

            await _unitOfWork.SaveChangesAsync(true, cancellationToken);

            _logger.LogInformation(
                "Outbox event marked as processed: Id={EventId}, Type={EventType}",
                eventId,
                eventEntity.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark outbox event as processed: Id={EventId}", eventId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task MarkAsFailedAsync(
        Guid eventId,
        string errorMessage,
        TimeSpan? nextAttemptDelay = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<OutboxEvent>();
            var eventEntity = await repository.GetAsync(eventId, cancellationToken: cancellationToken);
            
            if (eventEntity == null)
            {
                _logger.LogWarning("Outbox event not found: Id={EventId}", eventId);
                return;
            }

            eventEntity.RetryCount++;
            eventEntity.ErrorMessage = errorMessage;
            eventEntity.LockId = null;
            eventEntity.LockExpiresAt = null;

            // Если превышено максимальное количество попыток, помечаем как Failed окончательно
            if (eventEntity.RetryCount >= eventEntity.MaxRetryCount)
            {
                eventEntity.Status = OutboxEventStatus.Failed;
                eventEntity.NextAttemptAt = null;

                _logger.LogError(
                    "Outbox event failed after {RetryCount} attempts: Id={EventId}, Type={EventType}, Error={ErrorMessage}",
                    eventEntity.RetryCount,
                    eventId,
                    eventEntity.EventType,
                    errorMessage);
            }
            else
            {
                // Вычисляем время следующей попытки с экспоненциальной задержкой
                var delay = nextAttemptDelay ?? TimeSpan.FromMinutes(Math.Pow(2, eventEntity.RetryCount));
                eventEntity.NextAttemptAt = DateTime.UtcNow.Add(delay);
                eventEntity.Status = OutboxEventStatus.Pending;

                _logger.LogWarning(
                    "Outbox event failed, retry {RetryCount}/{MaxRetryCount} scheduled at {NextAttempt}: Id={EventId}, Type={EventType}, Error={ErrorMessage}",
                    eventEntity.RetryCount,
                    eventEntity.MaxRetryCount,
                    eventEntity.NextAttemptAt,
                    eventId,
                    eventEntity.EventType,
                    errorMessage);
            }

            await _unitOfWork.SaveChangesAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark outbox event as failed: Id={EventId}", eventId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task ReleaseLockAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<OutboxEvent>();
            var eventEntity = await repository.GetAsync(eventId, cancellationToken: cancellationToken);
            
            if (eventEntity == null)
            {
                _logger.LogWarning("Outbox event not found: Id={EventId}", eventId);
                return;
            }

            eventEntity.LockId = null;
            eventEntity.LockExpiresAt = null;
            eventEntity.Status = OutboxEventStatus.Pending;

            await _unitOfWork.SaveChangesAsync(true, cancellationToken);

            _logger.LogInformation("Released lock for outbox event: Id={EventId}", eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release lock for outbox event: Id={EventId}", eventId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> CleanupProcessedEventsAsync(int olderThanDays = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<OutboxEvent>();
            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);

            var eventsToDelete = await repository.GetRangeAsync(
                options: new Domain.Core.Dal.Repository.Models.QueryOptions<OutboxEvent>(true)
                    .AddFilter(e =>
                        e.Status == OutboxEventStatus.Processed &&
                        e.ProcessedAt != null &&
                        e.ProcessedAt < cutoffDate),
                cancellationToken: cancellationToken);

            if (!eventsToDelete.Any())
            {
                return 0;
            }

            await repository.RemoveRangeAsync(eventsToDelete, hard: true, cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(true, cancellationToken);

            _logger.LogInformation(
                "Cleaned up {Count} processed outbox events older than {Days} days",
                eventsToDelete.Count,
                olderThanDays);

            return eventsToDelete.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup processed outbox events");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> ReleaseExpiredLocksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<OutboxEvent>();
            var now = DateTime.UtcNow;

            var eventsWithExpiredLocks = await repository.GetRangeAsync(
                options: new Domain.Core.Dal.Repository.Models.QueryOptions<OutboxEvent>(true)
                    .AddFilter(e =>
                        e.LockExpiresAt != null &&
                        e.LockExpiresAt <= now),
                cancellationToken: cancellationToken);

            if (!eventsWithExpiredLocks.Any())
            {
                return 0;
            }

            foreach (var eventEntity in eventsWithExpiredLocks)
            {
                eventEntity.LockId = null;
                eventEntity.LockExpiresAt = null;
                eventEntity.Status = OutboxEventStatus.Pending;
            }

            await _unitOfWork.SaveChangesAsync(true, cancellationToken);

            _logger.LogInformation(
                "Released {Count} expired locks on outbox events",
                eventsWithExpiredLocks.Count);

            return eventsWithExpiredLocks.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release expired locks");
            throw;
        }
    }
}

