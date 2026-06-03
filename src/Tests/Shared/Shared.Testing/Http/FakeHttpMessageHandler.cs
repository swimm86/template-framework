// ----------------------------------------------------------------------------------------------
// <copyright file="FakeHttpMessageHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Shared.Testing.Http;

/// <summary>
/// Поддельный <see cref="HttpMessageHandler"/> для тестирования HTTP-клиентов.
/// </summary>
/// <remarks>
/// Поддерживает очередь предопределённых ответов, фиксацию запросов и опциональное
/// исключение при отправке. Используется для unit-тестов Bff HTTP-клиентов без сети.
/// </remarks>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<HttpResponseMessage> _responses = new();
    private readonly List<HttpRequestMessage> _requests = new();
    private readonly List<string> _requestBodies = new();

    /// <summary>
    /// Последний отправленный HTTP-запрос.
    /// </summary>
    public HttpRequestMessage? LastRequest => _requests.LastOrDefault();

    /// <summary>
    /// Содержимое тела последнего отправленного запроса в виде строки.
    /// </summary>
    public string? LastRequestBody => _requestBodies.LastOrDefault();

    /// <summary>
    /// Количество отправленных HTTP-запросов.
    /// </summary>
    public int RequestCount => _requests.Count;

    /// <summary>
    /// Коллекция всех отправленных HTTP-запросов (только для чтения).
    /// </summary>
    public IReadOnlyList<HttpRequestMessage> Requests => _requests.AsReadOnly();

    /// <summary>
    /// Исключение, которое будет выброшено при следующей отправке запроса.
    /// </summary>
    public Exception? ExceptionToThrow { get; set; }

    /// <summary>
    /// Добавляет предопределённый ответ в очередь.
    /// </summary>
    /// <param name="response">HTTP-ответ, который будет возвращён при следующей отправке.</param>
    public void QueueResponse(HttpResponseMessage response) => _responses.Enqueue(response);

    /// <summary>
    /// Добавляет JSON-ответ с сериализованной полезной нагрузкой в очередь.
    /// </summary>
    /// <typeparam name="T">Тип сериализуемой полезной нагрузки.</typeparam>
    /// <param name="payload">Объект для сериализации в JSON.</param>
    /// <param name="statusCode">HTTP-статус ответа. По умолчанию <see cref="HttpStatusCode.OK"/>.</param>
    public void QueueJsonResponse<T>(T payload, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(payload);
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        QueueResponse(response);
    }

    /// <summary>
    /// Добавляет ответ с указанным HTTP-статусом и пустым телом.
    /// </summary>
    /// <param name="statusCode">HTTP-статус ответа.</param>
    public void QueueEmptyResponse(HttpStatusCode statusCode)
    {
        QueueResponse(new HttpResponseMessage(statusCode));
    }

    /// <summary>
    /// Очищает очередь ответов и историю запросов.
    /// </summary>
    public void Reset()
    {
        while (_responses.TryDequeue(out _))
        {
        }

        _requests.Clear();
        _requestBodies.Clear();
        ExceptionToThrow = null;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        _requests.Add(request);

        if (request.Content is not null)
        {
            _requestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
        }

        if (_responses.TryDequeue(out var response))
        {
            return response;
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        };
    }
}
