// ----------------------------------------------------------------------------------------------
// <copyright file="DbContextBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Shared.Infrastructure.Dal.EFCore.Conventions;

namespace Shared.Infrastructure.Dal.EFCore;

/// <summary>
/// Базовый класс для <see cref="DbContext"/>.
/// </summary>
public abstract class DbContextBase(
    DbContextOptions options,
    IHostEnvironment environment
) : DbContext(options)
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

        ConfigureDateCulture();
        base.OnConfiguring(optionsBuilder);
    }

    private void ConfigureDateCulture()
    {
        var cultureInfo = new CultureInfo("en-US")
        {
            DateTimeFormat = { ShortDatePattern = "dd/MM/yyyy" }
        };

        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
    }
}
