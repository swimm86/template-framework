// ----------------------------------------------------------------------------------------------
// <copyright file="HttpOutboxEventHandler.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Outbox.Interfaces;
using Shared.Domain.Core.Entities;

namespace Shared.Application.Core.Outbox.Handlers;

/// <summary>
/// Обработчик HTTP запросов для Outbox событий.
/// </summary>
public class HttpOutboxEventHandler : IOutboxEventHandler
{
    /// <summary>
    /// Префикс типа события для HTTP запросов.
    /// </summary>
    public const string HttpEventTypePrefix = "http.";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpOutboxEventHandler> _logger;

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="httpClientFactory">Фабрика HTTP клиентов.</param>
    /// <param name="logger">Логгер.</param>
    public HttpOutboxEventHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<HttpOutboxEventHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool CanHandle(string eventType)
    {
        return eventType.StartsWith(HttpEventTypePrefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public async Task HandleAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(outboxEvent.Url))
        {
            throw new InvalidOperationException("URL is required for HTTP outbox events");
        }

        if (string.IsNullOrEmpty(outboxEvent.HttpMethod))
        {
            throw new InvalidOperationException("HTTP method is required for HTTP outbox events");
        }

        _logger.LogInformation(
            "Processing HTTP outbox event: {Method} {Url}, CorrelationId={CorrelationId}",
            outboxEvent.HttpMethod,
            outboxEvent.Url,
            outboxEvent.CorrelationId);

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(outboxEvent.TimeoutSeconds);

        // Создаем HTTP запрос
        var request = new HttpRequestMessage(
            new HttpMethod(outboxEvent.HttpMethod),
            outboxEvent.Url);

        // Добавляем тело запроса, если есть
        if (!string.IsNullOrEmpty(outboxEvent.EventData))
        {
            var contentType = outboxEvent.ContentType ?? "application/json";
            request.Content = new StringContent(
                outboxEvent.EventData,
                Encoding.UTF8,
                contentType);
        }

        // Добавляем заголовки
        if (!string.IsNullOrEmpty(outboxEvent.HeadersJson))
        {
            try
            {
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(outboxEvent.HeadersJson);
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse headers JSON");
            }
        }

        // Добавляем корреляционный идентификатор
        if (!string.IsNullOrEmpty(outboxEvent.CorrelationId))
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", outboxEvent.CorrelationId);
        }

        // Добавляем идентификатор идемпотентности
        if (!string.IsNullOrEmpty(outboxEvent.IdempotencyKey))
        {
            request.Headers.TryAddWithoutValidation("X-Idempotency-Key", outboxEvent.IdempotencyKey);
        }

        // Добавляем трассировочный идентификатор
        if (!string.IsNullOrEmpty(outboxEvent.TraceId))
        {
            request.Headers.TryAddWithoutValidation("X-Trace-Id", outboxEvent.TraceId);
        }

        // Отправляем запрос
        var response = await httpClient.SendAsync(request, cancellationToken);

        // Проверяем успешность
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"HTTP request failed with status code {(int)response.StatusCode} ({response.StatusCode}): {responseBody}");
        }

        _logger.LogInformation(
            "Successfully processed HTTP outbox event: {Method} {Url}, Status={StatusCode}",
            outboxEvent.HttpMethod,
            outboxEvent.Url,
            response.StatusCode);
    }
}

