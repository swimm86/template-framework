// ----------------------------------------------------------------------------------------------
// <copyright file="ApplicationBuilderExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Shared.Presentation.Core.CorrelationId.Middlewares;

namespace Shared.Presentation.Core.CorrelationId.Extensions;

/// <summary>
/// Содержит методы расширения <see cref="IApplicationBuilder"/>.
/// </summary>
internal static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Добавляет генерацию идентификатора корреляции для HTTP-запросов в случае их отсутствия.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/>.</param>
    /// <returns><see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseCorrelationId(
        this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
