// ----------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Shared.Common.Extensions;
using Shared.Common.Helpers;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Shared.Presentation.Core.Swagger;

/// <summary>
/// Содержит методы расширения для регистрации и настройки Swagger.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет сервисы генерации Swagger.
    /// </summary>
    /// <param name="services">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <param name="assemblyNamePrefix">Префикс имени проекта, по которому грузить xml файлы. </param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddSwagger(
        this IServiceCollection services,
        string? assemblyNamePrefix = null)
    {
        return services
            .AddSwaggerGen(options => ConfigureSwaggerGenOptions(options, assemblyNamePrefix))
            .Configure<ForwardedHeadersOptions>(options =>
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
    }

    /// <summary>
    /// Подключает middleware Swagger и настраивает Swagger UI.
    /// </summary>
    /// <param name="app">Конвейер приложения <see cref="IApplicationBuilder"/>.</param>
    /// <param name="setupUiAction">Делегат для настройки <see cref="SwaggerUIOptions"/>.</param>
    /// <returns>Текущий конвейер приложения <see cref="IApplicationBuilder"/>.</returns>
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

    /// <summary>
    /// Настраивает параметры генерации Swagger.
    /// </summary>
    /// <param name="options">Параметры генерации Swagger.</param>
    /// <param name="assemblyNamePrefix">Префикс имени проекта, по которому грузить xml файлы.</param>
    private static void ConfigureSwaggerGenOptions(SwaggerGenOptions options, string? assemblyNamePrefix = null)
    {
        List<string> xmlFiles = [.. Directory.GetFiles(AppContext.BaseDirectory, $"{assemblyNamePrefix ?? GetPrefix()}*.xml", SearchOption.TopDirectoryOnly)];
        if (xmlFiles.Count == 0)
        {
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, Assembly.GetEntryAssembly()!.GetName().Name + ".xml"), true);
        }
        else
        {
            xmlFiles.ForEach(xmlFile => options.IncludeXmlComments(xmlFile, true));
        }

        var documentationsPaths = AppDomain.CurrentDomain.GetAssemblies()
            .Select(x => Path.Combine(AppContext.BaseDirectory, $"{x.GetName().Name}.xml"))
            .Where(Path.Exists)
            .ToArray();
        documentationsPaths.ForEach(xmlFile => options.IncludeXmlComments(xmlFile, true));
        options.SupportNonNullableReferenceTypes();
        options.SchemaFilter<RequiredNotNullableSchemaFilter>();
        options.SchemaFilter<EnumTypesSchemaFilter>(new[] { documentationsPaths });
    }

    /// <summary>
    /// Получение префикса имени проекта.
    /// </summary>
    /// <returns>префикса имени проекта.</returns>
    private static string? GetPrefix()
    {
        var prefix = AssemblyHelper.GetModuleName();
        var pos = prefix.LastIndexOf('.');
        if (pos > 0)
        {
            prefix = prefix[..pos];
        }
        else
        {
            return null;
        }

        return prefix;
    }
}
