// ----------------------------------------------------------------------------------------------
// <copyright file="ApplicationBuilderExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Shared.Presentation.Core.Swagger.Extensions;

/// <summary>
/// Содержит методы расширения <see cref="IApplicationBuilder"/>.
/// </summary>
internal static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Активирует Swagger и настраивает SwaggerUI в пайплайне обработки запросов.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/>.</param>
    /// <param name="setupUiAction">Делегат для настройки <see cref="SwaggerUIOptions"/>.</param>
    /// <returns><see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseSwaggerConfigured(
        this IApplicationBuilder app,
        Action<SwaggerUIOptions>? setupUiAction = null)
    {
        app.UseSwagger();
        app.UseSwaggerUI(opts =>
        {
            opts.DocExpansion(DocExpansion.None);
            opts.ConfigObject.AdditionalItems.Add("tagsSorter", "alpha");

            setupUiAction?.Invoke(opts);
        });

        return app;
    }
}