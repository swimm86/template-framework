// ----------------------------------------------------------------------------------------------
// <copyright file="IOutboxService.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Entities;

namespace Shared.Application.Core.Outbox.Interfaces;

/// <summary>
/// Интерфейс сервиса для работы с Outbox событиями.
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// Добавляет новое событие в Outbox.
    /// </summary>
    /// <param name="outboxEvent">Событие для добавления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Добавленное событие.</returns>
    Task<OutboxEvent> AddAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавляет несколько событий в Outbox.
    /// </summary>
    /// <param name="outboxEvents">События для добавления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task AddRangeAsync(IEnumerable<OutboxEvent> outboxEvents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает события, готовые к обработке.
    /// </summary>
    /// <param name="batchSize">Размер батча для обработки.</param>
    /// <param name="lockId">Идентификатор блокировки для конкурентной обработки.</param>
    /// <param name="lockDurationMinutes">Длительность блокировки в минутах.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Список событий, готовых к обработке.</returns>
    Task<List<OutboxEvent>> GetPendingEventsAsync(
        int batchSize = 100,
        string? lockId = null,
        int lockDurationMinutes = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Помечает событие как обработанное успешно.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task MarkAsProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Помечает событие как неудачно обработанное.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="errorMessage">Сообщение об ошибке.</param>
    /// <param name="nextAttemptDelay">Задержка до следующей попытки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task MarkAsFailedAsync(
        Guid eventId,
        string errorMessage,
        TimeSpan? nextAttemptDelay = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Снимает блокировку с события.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task ReleaseLockAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Очищает устаревшие обработанные события.
    /// </summary>
    /// <param name="olderThanDays">Удалить события старше указанного количества дней.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Количество удаленных событий.</returns>
    Task<int> CleanupProcessedEventsAsync(int olderThanDays = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Снимает блокировки с просроченных событий.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Количество событий с снятыми блокировками.</returns>
    Task<int> ReleaseExpiredLocksAsync(CancellationToken cancellationToken = default);
}

