// ----------------------------------------------------------------------------------------------
// <copyright file="SeedConfigurationBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Application.Core.Dal.DbSeeder.Entities;

namespace Shared.Infrastructure.Dal.EFCore.Configurations;

/// <summary>
/// Конфигурация для сущности "Seed".
/// </summary>
public abstract class SeedConfigurationBase
    : IEntityTypeConfiguration<Seed>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Seed> builder)
    {
        builder.ToTable("seed", t => t.HasComment("Таблица с сущностями \"Сид БД\"."));
        builder.HasKey(x => x.Name);
        builder.Property(x => x.Name)
            .HasComment("Уникальное наименование сида БД.");
    }
}
