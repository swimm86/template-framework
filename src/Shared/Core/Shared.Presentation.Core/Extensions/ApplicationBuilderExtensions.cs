// ----------------------------------------------------------------------------------------------
// <copyright file="ApplicationBuilderExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Shared.Common.Logging;
using Shared.Presentation.Core.CorrelationId.Extensions;
using Shared.Presentation.Core.Swagger.Extensions;

namespace Shared.Presentation.Core.Extensions;

/// <summary>
/// Методы расширения для настройки пайплайна HTTP-запросов <see cref="IApplicationBuilder"/>.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Конфигурирует <see cref="IApplicationBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Метод настраивает пайплайн обработки HTTP-запросов: логирование, Correlation ID,
    /// Swagger (для среды разработки), обработку исключений, авторизацию и маршрутизацию контроллеров.
    /// </remarks>
    /// <param name="app"><see cref="WebApplication"/>.</param>
    /// <returns><see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UsePresentationCore(
        this WebApplication app)
    {
        LoggingServiceAccessor.Configure(app.Services);

        app.UseCorrelationId();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerConfigured();
        }

        app.UseExceptionHandler();

        // Configure the HTTP request pipeline.
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
