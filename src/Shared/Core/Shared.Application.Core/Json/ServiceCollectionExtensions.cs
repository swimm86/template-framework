// ----------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Application.Core.Json;

/// <summary>
/// Содержит методы расширения <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Конфигурация сериализатора JSON.
    /// </summary>
    /// <param name="services">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <returns>Коллекция сервисов <see cref="IServiceCollection"/>.</returns>
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

