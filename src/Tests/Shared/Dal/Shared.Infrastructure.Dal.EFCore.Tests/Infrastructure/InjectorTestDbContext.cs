// ----------------------------------------------------------------------------------------------
// <copyright file="InjectorTestDbContext.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

public sealed class InjectorTestDbContext(
    DbContextOptions<InjectorTestDbContext> options,
    IHostEnvironment environment)
    : DbContextBase(options, environment)
{
    public DbSet<InjectorTestEntity> Entities => Set<InjectorTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<InjectorTestEntity>(e => e.HasKey(x => x.Id));
    }
}

public sealed class InjectorTestEntity
{
    public Guid Id { get; set; }
}
