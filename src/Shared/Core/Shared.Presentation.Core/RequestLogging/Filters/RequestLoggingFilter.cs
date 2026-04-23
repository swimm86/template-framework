// ----------------------------------------------------------------------------------------------
// <copyright file="RequestLoggingFilter.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Presentation.Core.RequestLogging.Attributes;
using Shared.Presentation.Core.RequestLogging.Settings;

namespace Shared.Presentation.Core.RequestLogging.Filters;

/// <summary>
/// Фильтр для логирования аргументов контроллера.
/// </summary>
/// <remarks>
/// Выполняется ПОСЛЕ модель биндинга — все аргументы уже в памяти.
/// НЕ требует буферизации потока.
/// Сохраняет аргументы в HttpContext.Items для ExceptionHandler.
/// </remarks>
public sealed class RequestLoggingFilter(
    ILogger<RequestLoggingFilter> logger)
    : IAsyncActionFilter
{
    /// <summary>
    /// Информация о типе для сериализации.
    /// </summary>
    /// <param name="Properties">Массив свойств, которые необходимо сериализовать.</param>
    /// <param name="RedactedProperties">Массив свойств, которые необходимо проинициализировать заглушками.</param>
    private sealed record TypeSerializationInfo(
        PropertyInfo[] Properties,
        Dictionary<PropertyInfo, string> RedactedProperties)
    {
        /// <summary>
        /// Признак необходимости кастомной сериализации.
        /// </summary>
        /// <value>
        /// <c>true</c> — необходима сериализация согласно <see cref="Properties"/> и <see cref="RedactedProperties"/>;
        /// <c>false</c> — прямая сериализация в JSON.
        /// </value>
        internal bool CustomSerialization => Properties.Any() || RedactedProperties.Any();
    }

    /// <summary>
    /// Ключ для доступа к аргументам контроллера.
    /// </summary>
    public const string RequestArgumentsKey = nameof(RequestArgumentsKey);

    private const string FormFilePlaceholder = "<file>";
    private const string RedactedPlaceholder = "***";

    /// <summary>
    /// Кэш информации о типах для сериализации.
    /// null — тип не содержит IFormFile или [DoNotLog], сериализуем как есть.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, TypeSerializationInfo?> SerializationCache = [];

    /// <summary>
    /// Статические настройки фильтра.
    /// </summary>
    private static readonly RequestLoggingSettings Settings = CreateSettings();

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        MaxDepth = Settings.MaxDepth
    };

    /// <summary>
    /// Очищает кэш. Используется для тестов.
    /// </summary>
    public static void ClearCache() => SerializationCache.Clear();

    /// <summary>
    /// Перехватывает выполнение действия для логирования аргументов.
    /// </summary>
    /// <param name="context">Контекст выполнения действия.</param>
    /// <param name="next">Делегат для выполнения следующего фильтра или действия.</param>
    /// <returns>Task выполнения.</returns>
    public Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        // Если фильтр отключен — пропускаем
        if (!Settings.IsEnabled)
        {
            return next();
        }

        try
        {
            var arguments = PrepareArguments(context.ActionArguments);
            context.HttpContext.Items[RequestArgumentsKey] = arguments;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось извлечь аргументы контроллера для логирования");
        }

        return next();
    }

    /// <summary>
    /// Обрабатывает объект: заменяет IFormFile и [DoNotLog] на заглушки.
    /// </summary>
    private static object? ProcessObject(
        object? value)
    {
        if (value == null)
        {
            return null;
        }

        var type = value.GetType();
        var info = GetTypeSerializationInfo(type);
        if (!info.CustomSerialization)
        {
            return value;
        }

        // Создаём Dictionary с заменами — без LINQ для производительности
        var result = new Dictionary<string, object?>(
            info.Properties.Length + info.RedactedProperties.Count);

        // Добавляем свойства для прямой сериализации
        foreach (var prop in info.Properties)
        {
            result[prop.Name] = prop.GetValue(value);
        }

        // Добавляем свойства с заглушками
        foreach (var kvp in info.RedactedProperties)
        {
            result[kvp.Key.Name] = kvp.Value;
        }

        return result;
    }

    private static TypeSerializationInfo GetTypeSerializationInfo(
        Type type)
    {
        return SerializationCache.GetOrAdd(
            type,
            t =>
            {
                var allProperties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var propertiesInfo = allProperties
                    .Select(property => new
                    {
                        property,
                        notLoggable = property.GetCustomAttribute<DoNotLogAttribute>() != null,
                        formFile = property.PropertyType == typeof(IFormFile),
                    })
                    .ToArray();
                var customSerializationProperties = propertiesInfo
                    .Where(x => x.notLoggable || x.formFile)
                    .ToDictionary(
                        x => x.property,
                        x => x.formFile
                            ? FormFilePlaceholder
                            : RedactedPlaceholder);
                var propertiesToSerialize = customSerializationProperties.Any()
                    ? allProperties.Except(customSerializationProperties.Keys).ToArray()
                    : [];

                return new TypeSerializationInfo(propertiesToSerialize, customSerializationProperties);
            })!;
    }

    private static RequestLoggingSettings CreateSettings()
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var settings = configuration.GetOptions<RequestLoggingSettings>();
        return settings ?? new();
    }

    /// <summary>
    /// Подготавливает аргументы контроллера для логирования.
    /// </summary>
    private string? PrepareArguments(IDictionary<string, object?> arguments)
    {
        if (arguments.Count == 0)
        {
            return null;
        }

        var filtered = new Dictionary<string, object?>(arguments.Count);

        foreach (var arg in arguments)
        {
            // Пропускаем CancellationToken и IFormFileCollection
            if (arg.Value is CancellationToken or IFormFileCollection)
            {
                continue;
            }

            // Заменяем IFormFile на заглушку
            if (arg.Value is IFormFile)
            {
                filtered[arg.Key] = FormFilePlaceholder;
                continue;
            }

            // Обрабатываем сложные объекты
            try
            {
                filtered[arg.Key] = ProcessObject(arg.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка обработки аргумента {ArgumentName}", arg.Key);
                filtered[arg.Key] = $"<error: {arg.Value?.GetType().Name ?? "null"}>";
            }
        }

        // Сериализуем в JSON
        var json = JsonSerializer.Serialize(filtered, SerializerOptions);

        // Проверяем размер
        if (json.Length > Settings.MaxJsonPayloadLength)
        {
            logger.LogWarning(
                "Слишком большой payload аргументов: {Length} байт (макс. {Max}). Возвращена ошибка.",
                json.Length,
                Settings.MaxJsonPayloadLength);

            // Возвращаем валидный JSON с ошибкой вместо усечения
            return JsonSerializer.Serialize(
                new
                {
                    error = "payload_too_large",
                    length = json.Length,
                    maxAllowed = Settings.MaxJsonPayloadLength,
                },
                SerializerOptions);
        }

        return json;
    }
}
