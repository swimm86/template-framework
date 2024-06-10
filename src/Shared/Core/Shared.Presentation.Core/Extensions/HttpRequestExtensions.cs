// ----------------------------------------------------------------------------------------------
// <copyright file="HttpRequestExtensions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
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
    /// Возвзращает признак наличия куки в запросе по имени.
    /// </summary>
    /// <param name="request">Запрос.</param>
    /// <param name="cookieName">Название куки.</param>
    /// <returns>Признак наличия куки в запросе.</returns>
    public static bool ContainsCookie(this HttpRequest request, string cookieName) =>
        request.Cookies.ContainsKey(cookieName);

    /// <summary>
    /// Возвращает куки из запроса по имени (при наличии).
    /// </summary>
    /// <param name="request">Запрос.</param>
    /// <param name="cookieName">Название куки.</param>
    /// <param name="cookieValue">Значение куки.</param>
    /// <returns>Признак успешного выполнения операции.</returns>
    private static bool TryGetCookieValue(this HttpRequest request, string cookieName, out string? cookieValue) =>
        request.Cookies.TryGetValue(cookieName, out cookieValue);
}