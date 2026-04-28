// ----------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Application.Core.Json;

/// <summary>
/// Содержит методы расширения для настройки JSON-сериализации.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Настраивает JSON-сериализацию для Minimal API, Http-клиентов и MVC-контроллеров.
    /// </summary>
    /// <param name="services">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection ConfigureJsonSerializer(this IServiceCollection services)
    {
        // For minimal API
        services.ConfigureHttpJsonOptions(opts =>
            opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

        // For HTTP clients etc.
        services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(opts =>
            opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

        // For MVC/API controllers
        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(opts =>
            opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

        return services;
    }
}
