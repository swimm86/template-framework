// ----------------------------------------------------------------------------------------------
// <copyright file="OutboxEvent.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Entities;

/// <summary>
/// Сущность outbox события для хранения HTTP-запросов.
/// </summary>
/// <remarks>
/// Назначение основных полей:
/// - EventType: семантика события для маршрутизации/обработчика.
/// - EventData: тело события/HTTP-запроса (JSON).
/// - CorrelationId: сквозная корреляция между системами.
/// - Status: состояние обработки (Pending/Processing/Processed/Failed).
/// - CreatedAt/ProcessedAt: тайм-метки создания/завершения.
/// - RetryCount/ErrorMessage: ретраи и диагностика ошибок.
///
/// HTTP специфика:
/// - Url, HttpMethod, HeadersJson, ContentType: параметры HTTP-запроса.
/// - TimeoutSeconds, MaxRetryCount: таймаут и лимит ретраев для запроса.
/// - NextAttemptAt, Priority: планирование следующей попытки и приоритет.
/// - IdempotencyKey: защита от дублей на стороне получателя.
/// - TraceId: трассировка/обсервабилити.
/// - TenantId: мультиарендность/сегментация.
/// - LockId/LockExpiresAt: распределённая блокировка (lease) для конкурентных воркеров.
///
/// Рекомендуемые индексы (см. конфигурацию EF):
/// - Status, CreatedAt, CorrelationId;
/// - IdempotencyKey (UNIQUE);
/// - NextAttemptAt, Priority;
/// - составной (Status, NextAttemptAt) для планировщика.
/// </remarks>
public class OutboxEvent : IEntity<Guid>
{
    /// <summary>
    /// Уникальный идентификатор события. Присваивается при создании сущности.
    /// Используется как первичный ключ и для трассировки.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Тип события, необходимый для маршрутизации обработчиком (например, имя бизнес-события
    /// или типа HTTP-задачи).
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Данные события в формате JSON. Для HTTP — тело запроса (payload).
    /// Хранится в колонке типа text без ограничения длины.
    /// </summary>
    public string EventData { get; set; } = string.Empty;

    /// <summary>
    /// Идентификатор корреляции сквозной операции (опционально). Помогает связывать события
    /// в рамках одного бизнес-процесса.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Статус обработки события. Используется планировщиком и обработчиком для выбора задач.
    /// Возможные значения см. <see cref="OutboxEventStatus"/>.
    /// </summary>
    public OutboxEventStatus Status { get; set; } = OutboxEventStatus.Pending;

    /// <summary>
    /// Дата и время создания события (UTC). Устанавливается при инстанцировании.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTimeOffset.UtcNow.DateTime;

    /// <summary>
    /// Дата и время успешной обработки события (UTC). Заполняется при успешной отправке.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Количество попыток обработки (ретраев), выполненных по данному событию.
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Последнее сообщение об ошибке при обработке (если было).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Абсолютный URL назначения HTTP-запроса (например, https://api.example.com/path).
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// HTTP-метод (GET, POST, PUT, DELETE, ...).
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Заголовки HTTP-запроса в формате JSON (key/value). Хранятся как text.
    /// Рекомендуется валидировать сериализацию/десериализацию при использовании.
    /// </summary>
    public string? HeadersJson { get; set; }

    /// <summary>
    /// Content-Type тела запроса (например, application/json).
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Таймаут HTTP-запроса в секундах. Значение по умолчанию 100.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 100;

    /// <summary>
    /// Максимально допустимое количество повторных попыток доставки для данного события.
    /// Значение по умолчанию 5.
    /// </summary>
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>
    /// Дата и время (UTC) планируемой следующей попытки обработки. Используется планировщиком
    /// для отложенных ретраев и backoff-стратегии.
    /// </summary>
    public DateTime? NextAttemptAt { get; set; }

    /// <summary>
    /// Приоритет обработки: большее значение означает более высокий приоритет.
    /// Значение по умолчанию 0.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Ключ идемпотентности для предотвращения дублей на стороне обработчика/получателя.
    /// В БД имеет уникальный индекс.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Трассировочный идентификатор запроса для систем наблюдаемости (tracing).
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Идентификатор арендатора (для мультиарендных сценариев).
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Идентификатор распределённой блокировки (lease), используемый при конкурентной обработке
    /// несколькими воркерами. Присваивается при «захвате» задачи.
    /// </summary>
    public string? LockId { get; set; }

    /// <summary>
    /// Дата и время (UTC) истечения блокировки (lease). Позволяет автоматически снимать блокировку
    /// при аварийном завершении обработчика.
    /// </summary>
    public DateTime? LockExpiresAt { get; set; }

    /// <summary>
    /// Конструктор по умолчанию.
    /// </summary>
    public OutboxEvent()
    {
    }

    /// <summary>
    /// Конструктор с параметрами.
    /// </summary>
    /// <param name="eventType">Тип события.</param>
    /// <param name="eventData">Данные события.</param>
    /// <param name="correlationId">Идентификатор корреляции.</param>
    public OutboxEvent(string eventType, string eventData, string? correlationId = null)
    {
        Id = Guid.NewGuid();
        EventType = eventType;
        EventData = eventData;
        CorrelationId = correlationId;
        Status = OutboxEventStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        RetryCount = 0;
    }
}
