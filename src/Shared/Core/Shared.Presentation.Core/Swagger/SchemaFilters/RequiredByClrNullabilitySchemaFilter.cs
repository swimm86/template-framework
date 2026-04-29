// ----------------------------------------------------------------------------------------------
// <copyright file="RequiredByClrNullabilitySchemaFilter.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shared.Presentation.Core.Swagger.SchemaFilters;

/// <summary>
/// Синхронизирует <c>required</c> с CLR-nullability для всех свойств схемы.
/// </summary>
/// <remarks>
/// Учитывает:
/// <list type="bullet">
/// <item>non-nullable value types (Guid, int, bool и т.д.) как required;</item>
/// <item>nullable value types (Guid?, int? и т.д.) как optional;</item>
/// <item>nullable reference types по метаданным компилятора;</item>
/// <item>JSON-имена свойств (включая <see cref="JsonPropertyNameAttribute"/> и camelCase).</item>
/// </list>
/// </remarks>
public sealed class RequiredByClrNullabilitySchemaFilter
    : ISchemaFilter
{
    private static readonly NullabilityInfoContext NullabilityContext = new();

    /// <inheritdoc />
    public void Apply(
        IOpenApiSchema schema,
        SchemaFilterContext context)
    {
        if (schema.Properties == null || schema.Required == null || context.Type == null)
        {
            return;
        }

        var typeProperties = context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead)
            .ToArray();

        var schemaPropertyNames = schema.Properties.Keys
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var requiredByNullability = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var schemaPropertyName in schemaPropertyNames)
        {
            var typeProperty = FindTypeProperty(typeProperties, schemaPropertyName);
            if (typeProperty != null && IsRequired(typeProperty))
            {
                requiredByNullability.Add(schemaPropertyName);
            }
        }

        SynchronizeRequired(schema.Required, schemaPropertyNames, requiredByNullability);
    }

    private static bool IsRequired(PropertyInfo property)
    {
        var type = property.PropertyType;

        // Value type required, кроме Nullable<T>.
        if (type.IsValueType)
        {
            return Nullable.GetUnderlyingType(type) == null;
        }

        // Для reference types используем NRT-метаданные.
        var nullabilityInfo = NullabilityContext.Create(property);
        return nullabilityInfo.ReadState == NullabilityState.NotNull;
    }

    private static PropertyInfo? FindTypeProperty(
        IEnumerable<PropertyInfo> typeProperties,
        string schemaPropertyName)
    {
        foreach (var property in typeProperties)
        {
            if (string.Equals(property.Name, schemaPropertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property;
            }

            var jsonPropertyName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
            if (!string.IsNullOrWhiteSpace(jsonPropertyName)
                && string.Equals(jsonPropertyName, schemaPropertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property;
            }

            var camelCaseName = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            if (string.Equals(camelCaseName, schemaPropertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property;
            }
        }

        return null;
    }

    private static void SynchronizeRequired(
        ISet<string> currentRequired,
        IReadOnlySet<string> schemaPropertyNames,
        IReadOnlySet<string> requiredByNullability)
    {
        // Убираем "висячие" required-ключи и nullable свойства.
        foreach (var requiredName in currentRequired.ToArray())
        {
            if (!schemaPropertyNames.Contains(requiredName) || !requiredByNullability.Contains(requiredName))
            {
                currentRequired.Remove(requiredName);
            }
        }

        // Добавляем отсутствующие required для non-nullable свойств.
        foreach (var requiredName in requiredByNullability)
        {
            currentRequired.Add(requiredName);
        }
    }
}
