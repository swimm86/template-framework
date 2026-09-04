// ----------------------------------------------------------------------------------------------
// <copyright file="TestEfRepositoryDbContext.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Dal.EFCore.Conventions;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

public class TestEfRepositoryDbContext(
    DbContextOptions<TestEfRepositoryDbContext> options)
    : DbContext(options)
{
    public DbSet<TestEntityWithCreatedDeleted> Entities => Set<TestEntityWithCreatedDeleted>();
    public DbSet<TestEfEntity> TestEfEntities => Set<TestEfEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntityWithCreatedDeleted>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<TestEfEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Add(_ => new ColumnsNamesConvention());
    }
}
