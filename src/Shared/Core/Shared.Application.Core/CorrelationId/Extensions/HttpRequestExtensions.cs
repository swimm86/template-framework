// ----------------------------------------------------------------------------------------------
// <copyright file="HttpRequestExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;

namespace Shared.Application.Core.CorrelationId.Extensions;

/// <summary>
/// Расширения для <see cref="HttpRequest"/>.
/// </summary>
public static class HttpRequestExtensions
{
    /// <summary>
    /// Получает идентификатор корреляции из HTTP заголовка запроса.
    /// </summary>
    /// <param name="request">HTTP запрос.</param>
    /// <returns>Идентификатор корреляции или <c>null</c>, если заголовок отсутствует или имеет невалидное значение.</returns>
    public static Guid? GetCorrelationId(this HttpRequest? request)
    {
        if (request?.Headers.TryGetValue(Constants.CorrelationIdHeader, out var headerValue) == true
            && !string.IsNullOrWhiteSpace(headerValue)
            && Guid.TryParse(headerValue.ToString(), out var correlationId))
        {
            return correlationId;
        }

        return null;
    }

    /// <summary>
    /// Добавляет идентификатор корреляции в HTTP заголовок запроса при отсутствии валидного значения.
    /// </summary>
    /// <param name="request">HTTP запрос.</param>
    /// <returns><c>true</c>, если идентификатор корреляции добавлен в заголовок запроса, иначе <c>false</c>.</returns>
    public static bool TryAddCorrelationId(this HttpRequest? request)
    {
        if (request == null || request.HasValidCorrelationId())
        {
            return false;
        }

        request.Headers[Constants.CorrelationIdHeader] = Guid.NewGuid().ToString("D");
        return true;
    }

    /// <summary>
    /// Проверяет наличие валидного идентификатора корреляции в HTTP заголовке запроса.
    /// </summary>
    /// <param name="request">HTTP запрос.</param>
    /// <returns><c>true</c>, если идентификатор корреляции присутствует в заголовках запроса, иначе <c>false</c>.</returns>
    private static bool HasValidCorrelationId(this HttpRequest? request)
    {
        return request?.Headers.TryGetValue(Constants.CorrelationIdHeader, out var correlationId) == true &&
               Guid.TryParse(correlationId, out _);
    }
}
