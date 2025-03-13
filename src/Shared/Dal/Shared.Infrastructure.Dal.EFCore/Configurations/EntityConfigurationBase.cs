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

        ConfigureDomainEvents(builder);
        ConfigureMeta(builder);
        ConfigureProcess(builder);
    }

    /// <summary>
    /// Основной метод для конфигурации сущности.
    /// </summary>
    /// <param name="builder">Построитель сущности.</param>
    protected virtual void ConfigureProcess(EntityTypeBuilder<TEntity> builder)
    {
    }

    private static void ConfigureDomainEvents(EntityTypeBuilder builder)
    {
        if (typeof(IWithDomainEvents).IsAssignableFrom(typeof(TEntity)))
        {
            builder.Ignore(nameof(IWithDomainEvents.RequiredToSaveNavigationPropertiesNames));
        }
    }

    private static void ConfigureMeta(EntityTypeBuilder builder)
    {
        if (typeof(IWithCreated).IsAssignableFrom(typeof(TEntity)))
        {
            builder
                .Property(nameof(IWithCreated.CreatedByUserId))
                .HasColumnName("created_by");

            builder
                .Property(nameof(IWithCreated.CreatedByUserName))
                .HasColumnName("created_by_user_name");
        }

        if (typeof(IWithUpdated).IsAssignableFrom(typeof(TEntity)))
        {
            builder
                .Property(nameof(IWithUpdated.UpdatedByUserId))
                .HasColumnName("updated_by");
        }

        if (typeof(IWithDeleted).IsAssignableFrom(typeof(TEntity)))
        {
            builder
                .Property(nameof(IWithDeleted.DeletedByUserId))
                .HasColumnName("deleted_by_id");
        }

        ConfigureDates(builder);
    }

    private static void ConfigureDates(EntityTypeBuilder builder)
    {
        if (typeof(IWithDateCreated).IsAssignableFrom(typeof(TEntity)))
        {
            builder
                .Property(nameof(IWithDateCreated.DateCreated))
                .IsRequired()
                .HasColumnName("created_at");
        }

        if (typeof(IWithDateUpdated).IsAssignableFrom(typeof(TEntity)))
        {
            builder
                .Property(nameof(IWithDateUpdated.DateUpdated))
                .IsRequired()
                .HasColumnName("updated_at");
        }

        if (typeof(IWithDateDeleted).IsAssignableFrom(typeof(TEntity)))
        {
            builder
                .Property(nameof(IWithDateDeleted.DateDeleted))
                .IsRequired()
                .HasColumnName("deleted_at");
        }
    }
}
