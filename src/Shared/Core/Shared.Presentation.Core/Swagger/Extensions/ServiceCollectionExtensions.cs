// ----------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Shared.Common.Extensions;
using Shared.Presentation.Core.Swagger.SchemaFilters;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shared.Presentation.Core.Swagger.Extensions;

/// <summary>
/// Содержит методы расширения для регистрации и настройки Swagger.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет сервисы генерации Swagger.
    /// </summary>
    /// <param name="services">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        return services
            .AddSwaggerGen(ConfigureSwaggerGenOptions)
            .Configure<ForwardedHeadersOptions>(options =>
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
    }

    /// <summary>
    /// Настраивает параметры генерации Swagger.
    /// </summary>
    /// <param name="options">Параметры генерации Swagger.</param>
    private static void ConfigureSwaggerGenOptions(SwaggerGenOptions options)
    {
        var documentationsPaths = AppDomain.CurrentDomain.GetAssemblies()
            .Select(x => Path.Combine(AppContext.BaseDirectory, $"{x.GetName().Name}.xml"))
            .Where(Path.Exists)
            .ToArray();
        documentationsPaths.ForEach(xmlFile => options.IncludeXmlComments(xmlFile, true));
        options.SupportNonNullableReferenceTypes();
        options.SchemaFilter<RequiredByClrNullabilitySchemaFilter>();
        options.SchemaFilter<EnumTypesSchemaFilter>(new[] { documentationsPaths });
    }
}
