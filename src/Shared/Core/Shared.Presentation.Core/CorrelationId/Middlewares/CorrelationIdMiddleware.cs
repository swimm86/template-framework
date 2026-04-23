// ----------------------------------------------------------------------------------------------
// <copyright file="CorrelationIdMiddleware.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Shared.Application.Core.CorrelationId.Extensions;
using CorrelationIdConstants = Shared.Application.Core.CorrelationId.Constants;

namespace Shared.Presentation.Core.CorrelationId.Middlewares;

/// <summary>
/// Middleware, гарантирующий наличие идентификатора корреляции в HTTP-запросе.
/// </summary>
public sealed class CorrelationIdMiddleware(
    RequestDelegate next)
{
    /// <summary>
    /// Выполняет middleware.
    /// </summary>
    /// <param name="context">Контекст HTTP-запроса.</param>
    /// <returns><see cref="Task"/>.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        context.Request.TryAddCorrelationId();
        context.Response.Headers[CorrelationIdConstants.CorrelationIdHeader] =
            context.Request.Headers[CorrelationIdConstants.CorrelationIdHeader];
        return next(context);
    }
}
