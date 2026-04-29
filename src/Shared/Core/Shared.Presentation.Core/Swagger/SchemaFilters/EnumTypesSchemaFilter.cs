// ----------------------------------------------------------------------------------------------
// <copyright file="EnumTypesSchemaFilter.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Xml.Linq;
using Microsoft.OpenApi;
using Shared.Common.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shared.Presentation.Core.Swagger.SchemaFilters;

/// <summary>
/// Дополняет описание enum-схем списком значений и их XML-описаний из документации сборок.
/// </summary>
/// <remarks>
/// Фильтр не меняет контракт схемы (type/enum/required), а только обогащает поле
/// <c>description</c> человекочитаемым списком "числовое значение - имя - summary".
/// </remarks>
/// <param name="xmlPaths">Абсолютные пути к XML-файлам документации, из которых читаются summary для членов enum.</param>
public class EnumTypesSchemaFilter(
    params string[] xmlPaths)
    : ISchemaFilter
{
    /// <summary>
    /// Предзагруженные XML-документы с комментариями, используемые для поиска описаний enum-значений.
    /// </summary>
    private readonly XDocument[] _xmlComments = xmlPaths.Select(XDocument.Load).ToArray();

    /// <summary>
    /// Добавляет HTML-список enum-значений в описание схемы.
    /// </summary>
    /// <param name="schema"><see cref="OpenApiSchema"/>.</param>
    /// <param name="context"><see cref="SchemaFilterContext"/>.</param>
    public void Apply(
        IOpenApiSchema schema,
        SchemaFilterContext context)
    {
        if (schema.Enum is not { Count: > 0 } || context.Type is not { IsEnum: true })
        {
            return;
        }

        schema.Description += "<p>Members:</p><ul>";
        schema.Enum
            .Where(x => x.GetValueKind() == JsonValueKind.Number)
            .Select(x => x.GetValue<int>())
            .ForEach(x =>
            {
                var valueName = Enum.GetName(context.Type, x);
                var fullTypeName = $"F:{context.Type.FullName}.{valueName}";
                var description = _xmlComments
                    .Descendants("member")
                    .FirstOrDefault(m =>
                        m.Attribute("name")?.Value.Equals(fullTypeName, StringComparison.OrdinalIgnoreCase) ?? false)?
                    .Descendants("summary")
                    .FirstOrDefault()?.Value
                    .Trim();

                schema.Description +=
                    $"<li><i>{x}</i> - {valueName}" +
                    $"{(string.IsNullOrWhiteSpace(description) ? string.Empty : $" ({description})")}</li>";
            });

        schema.Description += "</ul>";
    }
}
