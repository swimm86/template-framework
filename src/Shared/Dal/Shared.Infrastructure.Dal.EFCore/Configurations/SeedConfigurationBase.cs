// ----------------------------------------------------------------------------------------------
// <copyright file="SeedConfigurationBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
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
        builder.Property(x => x.Id)
            .HasComment("Уникальное наименование сида БД.");
    }
}
