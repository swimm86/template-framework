// ----------------------------------------------------------------------------------------------
// <copyright file="EntityConfigurationBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Core.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Configurations;

/// <summary>
/// Базовый абстрактный класс для конфигурации сущностей в EF Core.
/// </summary>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
public abstract class EntityConfigurationBase<TEntity>
    : IEntityTypeConfiguration<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// Метод для конфигурации сущности.
    /// </summary>
    /// <param name="builder">Построитель сущности.</param>
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        const string idName = nameof(IEntity.Id);
        builder.UseTptMappingStrategy();
        builder.HasKey(idName);
        builder.Property(idName).ValueGeneratedNever();

        ConfigureProcess(builder);
    }

    /// <summary>
    /// Основной метод для конфигурации сущности.
    /// </summary>
    /// <param name="builder">Построитель сущности.</param>
    protected virtual void ConfigureProcess(EntityTypeBuilder<TEntity> builder)
    {
    }
}
