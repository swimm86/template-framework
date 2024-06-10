// ----------------------------------------------------------------------------------------------
// <copyright file="EntityConfigurationBase.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Core.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Configurations;

public abstract class EntityConfigurationBase<TEntity>
    : IEntityTypeConfiguration<TEntity> where TEntity : class, IEntity
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        const string idName = nameof(IEntity.Id);
        builder.UseTptMappingStrategy();
        builder.HasKey(idName);
        builder.Property(idName).ValueGeneratedNever();

        ConfigureProcess(builder);
    }

    protected virtual void ConfigureProcess(EntityTypeBuilder<TEntity> builder)
    {
    }
}

//public class UserConfiguration : EntityConfigurationBase<User>
//{
//    protected override void ConfigureProcess(EntityTypeBuilder<User> builder)
//    {
//    }
//}
