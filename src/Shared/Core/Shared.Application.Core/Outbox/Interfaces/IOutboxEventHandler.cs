// ----------------------------------------------------------------------------------------------
// <copyright file="IOutboxEventHandler.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Entities;

namespace Shared.Application.Core.Outbox.Interfaces;

/// <summary>
/// Интерфейс обработчика Outbox событий.
/// </summary>
public interface IOutboxEventHandler
{
    /// <summary>
    /// Определяет, может ли обработчик обработать событие данного типа.
    /// </summary>
    /// <param name="eventType">Тип события.</param>
    /// <returns>True, если обработчик может обработать событие; иначе false.</returns>
    bool CanHandle(string eventType);

    /// <summary>
    /// Обрабатывает событие.
    /// </summary>
    /// <param name="outboxEvent">Событие для обработки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task HandleAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);
}

