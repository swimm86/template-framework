// ----------------------------------------------------------------------------------------------
// <copyright file="ColumnsNamesConvention.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Shared.Common.Extensions;

namespace Shared.Infrastructure.Dal.EFCore.Conventions;

/// <summary>
/// Конвенция для приведения названия полей к snake_case.
/// </summary>
public class ColumnsNamesConvention : IModelFinalizingConvention
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
                entity.GetProperties().ForEach(prop => prop.SetColumnName(prop.GetColumnName().ConvertToSnakeCase())));
    }
}
