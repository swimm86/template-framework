// ----------------------------------------------------------------------------------------------
// <copyright file="OutboxEventStatus.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Enums;

/// <summary>
/// Статус outbox события.
/// </summary>
public enum OutboxEventStatus
{
    /// <summary>
    /// Ожидает обработки.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Обрабатывается.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Обработано успешно.
    /// </summary>
    Processed = 2,

    /// <summary>
    /// Ошибка обработки.
    /// </summary>
    Failed = 3
}
