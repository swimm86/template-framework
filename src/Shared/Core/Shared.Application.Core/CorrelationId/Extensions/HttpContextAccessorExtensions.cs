// ----------------------------------------------------------------------------------------------
// <copyright file="HttpContextAccessorExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;

namespace Shared.Application.Core.CorrelationId.Extensions;

/// <summary>
/// Расширения для <see cref="IHttpContextAccessor"/>.
/// </summary>
public static class HttpContextAccessorExtensions
{
    /// <summary>
    /// Получает идентификатор корреляции из HTTP заголовка запроса.
    /// </summary>
    /// <param name="httpContextAccessor">Предоставляет доступ к текущему HTTP-запросу.</param>
    /// <returns>Идентификатор корреляции или <c>null</c>, если заголовок отсутствует.</returns>
    public static Guid? GetCorrelationId(this IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext?.Request.GetCorrelationId();
    }
}
