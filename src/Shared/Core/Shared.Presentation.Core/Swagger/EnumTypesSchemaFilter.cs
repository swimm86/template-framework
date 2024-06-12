// ----------------------------------------------------------------------------------------------
// <copyright file="EnumTypesSchemaFilter.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Xml.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Shared.Common.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shared.Presentation.Core.Swagger;

/// <summary>
/// Фильтр для документирования enum-ов.
/// </summary>
/// <param name="xmlPaths">Пути к xml файлам</param>
public class EnumTypesSchemaFilter(params string[] xmlPaths) : ISchemaFilter
{
    /// <summary>
    /// Загружает xml-файлы.
    /// </summary>
    private readonly XDocument[] _xmlComments = xmlPaths.Select(XDocument.Load).ToArray();

    /// <summary>
    /// Применяет фильтр.
    /// </summary>
    /// <param name="schema"><see cref="OpenApiSchema"/>.</param>
    /// <param name="context"><see cref="SchemaFilterContext"/>.</param>
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Enum is not { Count: > 0 } || context.Type is not { IsEnum: true })
        {
            return;
        }

        schema.Description += "<p>Members:</p><ul>";
        schema.Enum.OfType<OpenApiInteger>().Select(x => x.Value).ForEach(x =>
        {
            var valueName = Enum.GetName(context.Type, x);
            var fullTypeName = $"F:{context.Type.FullName}.{valueName}";
            var description = _xmlComments
                .Descendants("member")
                .FirstOrDefault(m =>
                    m.Attribute("name")?.Value.Equals(fullTypeName, StringComparison.OrdinalIgnoreCase) ?? false)?
                .Descendants("summary")
                .FirstOrDefault()?.Value;

            schema.Description +=
                $"<li><i>{x}</i> - {valueName}" +
                $"{(string.IsNullOrWhiteSpace(description) ? string.Empty : $" ({description})")}</li>";
        });

        schema.Description += "</ul>";
    }
}
