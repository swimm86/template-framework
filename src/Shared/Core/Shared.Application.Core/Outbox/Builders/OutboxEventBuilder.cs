// ----------------------------------------------------------------------------------------------
// <copyright file="OutboxEventBuilder.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text.Json;
using Shared.Domain.Core.Entities;

namespace Shared.Application.Core.Outbox.Builders;

/// <summary>
/// Builder для создания Outbox событий.
/// </summary>
public class OutboxEventBuilder
{
    private readonly OutboxEvent _outboxEvent;

    /// <summary>
    /// Конструктор.
    /// </summary>
    public OutboxEventBuilder()
    {
        _outboxEvent = new OutboxEvent();
    }

    /// <summary>
    /// Устанавливает тип события.
    /// </summary>
    /// <param name="eventType">Тип события.</param>
    /// <returns>Builder для цепочки вызовов.</returns>
    public OutboxEventBuilder WithEventType(string eventType)
    {
        _outboxEvent.EventType = eventType;
        return this;
    }

    /// <summary>
    /// Устанавливает данные события.
    /// </summary>
    /// <param name="eventData">Данные события.</param>
    /// <returns>Builder для цепочки вызовов.</returns>
    public OutboxEventBuilder WithEventData(string eventData)
    {
        _outboxEvent.EventData = eventData;
        return this;
    }

    /// <summary>
    /// Устанавливает данные события из объекта.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <param name="data">Объект для сериализации.</param>
    /// <returns>Builder для цепочки вызовов.</returns>
    public OutboxEventBuilder WithEventData<T>(T data)
    {
        _outboxEvent.EventData = JsonSerializer.Serialize(data);
        return this;
    }

    /// <summary>
    /// Устанавливает идентификатор корреляции.
    /// </summary>
    /// <param name="correlationId">Идентификатор корреляции.</param>
    /// <returns>Builder для цепочки вызовов.</returns>
    public OutboxEventBuilder WithCorrelationId(string correlationId)
    {
        _outboxEvent.CorrelationId = correlationId;
        return this;
    }

    /// <summary>
    /// Устанавливает приоритет.
    /// </summary>
    /// <param name="priority">Приоритет.</param>
    /// <returns>Builder для цепочки вызовов.</returns>
    public OutboxEventBuilder WithPriority(int priority)
    {
        _outboxEvent.Priority = priority;
        return this;
    }

    /// <summary>
    /// Устанавливает ключ идемпотентности.
    /// </summary>
    /// <param name="idempotencyKey">Ключ идемпотентности.</param>
    /// <returns>Builder для цепочки вызовов.</returns>
    public OutboxEventBuilder WithIdempotencyKey(string idempotencyKey)
    {
        _outboxEvent.IdempotencyKey = idempotencyKey;
        return this;
    }

    /// <summary>
    /// Устанавливает трассировочный идентификатор.
    /// </summary>
    /// <param name="traceId">Трассировочный идентификатор.</param>
    /// <returns>Builder для цепочки вызовов.</returns>
    public OutboxEventBuilder WithTraceId(string traceId)
    {
        _outboxEvent.TraceId = traceId;
        return this;
    }

    /// <summary>
    /// Устанавливает идентификатор арендатора.
    /// </summary>
    /// <param name="tenantId">Идентификатор арендатора.</param>
    /// <returns>Builder для цепочки вызовов.</returns>
    public OutboxEventBuilder WithTenantId(string tenantId)
    {
        _outboxEvent.TenantId = tenantId;
        return this;
    }

    /// <summary>
    /// Устанавливает максимальное количество попыток.
    /// </summary>
    /// <param name="maxRetryCount">Максимальное количество попыток.</param>
    /// <returns>Builder для цепочки вызовов.</returns>
    public OutboxEventBuilder WithMaxRetryCount(int maxRetryCount)
    {
        _outboxEvent.MaxRetryCount = maxRetryCount;
        return this;
    }

    /// <summary>
    /// Устанавливает время следующей попытки.
    /// </summary>
    /// <param name="nextAttemptAt">Время следующей попытки.</param>
    /// <returns>Builder для цепочки вызовов.</returns>
    public OutboxEventBuilder WithNextAttemptAt(DateTime nextAttemptAt)
    {
        _outboxEvent.NextAttemptAt = nextAttemptAt;
        return this;
    }

    /// <summary>
    /// Настраивает событие как HTTP запрос.
    /// </summary>
    /// <param name="method">HTTP метод.</param>
    /// <param name="url">URL.</param>
    /// <param name="contentType">Content type.</param>
    /// <returns>Builder для цепочки вызовов.</returns>
    public OutboxEventBuilder AsHttpRequest(string method, string url, string? contentType = "application/json")
    {
        _outboxEvent.HttpMethod = method;
        _outboxEvent.Url = url;
        _outboxEvent.ContentType = contentType;

        // Автоматически устанавливаем тип события для HTTP
        if (string.IsNullOrEmpty(_outboxEvent.EventType))
        {
            _outboxEvent.EventType = $"http.{method.ToLower()}";
        }

        return this;
    }

    /// <summary>
    /// Устанавливает заголовки HTTP запроса.
    /// </summary>
    /// <param name="headers">Заголовки.</param>
    /// <returns>Builder для цепочки вызовов.</returns>
    public OutboxEventBuilder WithHttpHeaders(Dictionary<string, string> headers)
    {
        _outboxEvent.HeadersJson = JsonSerializer.Serialize(headers);
        return this;
    }

    /// <summary>
    /// Устанавливает таймаут запроса в секундах.
    /// </summary>
    /// <param name="timeoutSeconds">Таймаут в секундах.</param>
    /// <returns>Builder для цепочки вызовов.</returns>
    public OutboxEventBuilder WithTimeout(int timeoutSeconds)
    {
        _outboxEvent.TimeoutSeconds = timeoutSeconds;
        return this;
    }

    /// <summary>
    /// Создает экземпляр OutboxEvent.
    /// </summary>
    /// <returns>Созданное событие.</returns>
    public OutboxEvent Build()
    {
        if (string.IsNullOrEmpty(_outboxEvent.EventType))
        {
            throw new InvalidOperationException("EventType is required");
        }

        if (string.IsNullOrEmpty(_outboxEvent.EventData))
        {
            throw new InvalidOperationException("EventData is required");
        }

        _outboxEvent.Id = Guid.NewGuid();
        _outboxEvent.CreatedAt = DateTime.UtcNow;

        return _outboxEvent;
    }
}

