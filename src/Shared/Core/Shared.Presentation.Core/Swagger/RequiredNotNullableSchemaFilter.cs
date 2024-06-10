// ----------------------------------------------------------------------------------------------
// <copyright file="RequiredNotNullableSchemaFilter.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shared.Presentation.Core.Swagger;

/// <summary>
/// Фильтр для того чтобы сделать non-nullable поля required: true в свагере.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class RequiredNotNullableSchemaFilter : ISchemaFilter
{
    /// <inheritdoc /> 
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null)
        {
            return;
        }

        var notNullableProperties = schema
            .Properties
            .Where(x => !x.Value.Nullable && !schema.Required.Contains(x.Key))
            .ToList();

        foreach (var property in notNullableProperties)
        {
            schema.Required.Add(property.Key);
        }
    }
}
