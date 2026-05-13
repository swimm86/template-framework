// ----------------------------------------------------------------------------------------------
// <copyright file="DbContext.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Shared.Infrastructure.Dal.EFCore;
using Template.Infrastructure.Dal.Conventions;

namespace Template.Infrastructure.Dal;

/// <summary>
/// Реализация <see cref="DbContext"/> для нашего приложения (общий для getter и setter).
/// </summary>
public class DbContext(
    DbContextOptions<DbContext> options,
    IHostEnvironment environment)
    : DbContextBase(options, environment)
{
    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Add(_ => new ColumnsNamesConvention());
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
