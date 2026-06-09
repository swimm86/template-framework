// ----------------------------------------------------------------------------------------------
// <copyright file="PersonConfigurations.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.Dal.EFCore.Configurations;
using Template.Domain.Entities;

namespace Template.Infrastructure.Dal.Configuration;

/// <summary>
/// Конфигурация сущности "Персона".
/// </summary>
public class PersonConfigurations
    : EntityConfigurationBase<Person>
{
    /// <inheritdoc />
    protected override void ConfigureProcess(EntityTypeBuilder<Person> builder)
    {
        base.ConfigureProcess(builder);

        builder.ToTable("person", t => t.HasComment("Таблица с сущностями \"Персона\"."));
        builder
            .Property(x => x.Name)
            .HasComment("Имя");

        builder
            .Property(x => x.Email)
            .HasComment("Адрес электронной почты");
    }
}
