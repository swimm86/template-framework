// ----------------------------------------------------------------------------------------------
// <copyright file="HttpRequestExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;

namespace Shared.Presentation.Core.Extensions;

/// <summary>
/// Расширения для <see cref="HttpRequest"/>.
/// </summary>
public static class HttpRequestExtensions
{
    /// <summary>
    /// Возвращает признак наличия cookie в запросе по имени.
    /// </summary>
    /// <param name="request">HTTP-запрос.</param>
    /// <param name="cookieName">Имя cookie.</param>
    /// <returns>Значение <c>true</c>, если cookie с указанным именем присутствует в запросе; иначе <c>false</c>.</returns>
    public static bool ContainsCookie(this HttpRequest request, string cookieName) =>
        request.Cookies.ContainsKey(cookieName);

    /// <summary>
    /// Получает значение cookie из запроса по имени.
    /// </summary>
    /// <param name="request">HTTP-запрос.</param>
    /// <param name="cookieName">Имя cookie.</param>
    /// <param name="cookieValue">Значение cookie, если оно найдено; иначе <c>null</c>.</param>
    /// <returns>Значение <c>true</c>, если cookie с указанным именем присутствует в запросе; иначе <c>false</c>.</returns>
    private static bool TryGetCookieValue(this HttpRequest request, string cookieName, out string? cookieValue) =>
        request.Cookies.TryGetValue(cookieName, out cookieValue);
}