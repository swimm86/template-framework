// ----------------------------------------------------------------------------------------------
// <copyright file="DbContextBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.Extensions.Hosting;
using Shared.Infrastructure.Dal.EFCore.Conventions;

namespace Shared.Infrastructure.Dal.EFCore;

/// <summary>
/// Базовый класс для <see cref="DbContext"/>.
/// </summary>
/// <remarks>
/// Предоставляет базовые conventions: snake_case имена колонок и автоприменение конфигураций.
/// </remarks>
public abstract class DbContextBase(
    DbContextOptions options,
    IHostEnvironment environment)
    : DbContext(options)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetCallingAssembly());
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Add(_ => new ColumnsNamesConvention());
    }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (environment.IsDevelopment())
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }

        base.OnConfiguring(optionsBuilder);
    }
}
