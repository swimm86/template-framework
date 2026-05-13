// ----------------------------------------------------------------------------------------------
// <copyright file="ColumnsNamesConvention.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Shared.Common.Extensions;

namespace Template.Infrastructure.Dal.Conventions;

/// <summary>
/// Конвенция для приведения названия полей к snake_case.
/// </summary>
public class ColumnsNamesConvention
    : IModelFinalizingConvention
{
    /// <inheritdoc />
    public void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        modelBuilder.Metadata
            .GetEntityTypes()
            .ToList()
            .ForEach(entity =>
                entity.GetProperties().ForEach(prop => prop.SetColumnName(prop.GetColumnName().ToSnakeCase())));
    }
}
