// ----------------------------------------------------------------------------------------------
// <copyright file="DbContextOptionsBuilderInitializer.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Application.Core.Dal.Settings.Models.Base;
using Shared.Infrastructure.Dal.EFCore.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Postgres;

/// <summary>
/// Реализация интерфейся <see cref="IDbContextOptionsBuilderInitializer"/> для Npgsql.
/// </summary>
public class DbContextOptionsBuilderInitializer(
    IConfiguration configuration)
    : IDbContextOptionsBuilderInitializer
{
    /// <inheritdoc />
    public void Initialize<TSettings>(
        DbContextOptionsBuilder options,
        string migrationAssemblyName)
        where TSettings : DbSettingsBase
    {
        var settings = configuration.GetOptions<TSettings>()!;
        var sourceBuilder = new NpgsqlDataSourceBuilder(settings.ConnectionString);
        options.UseNpgsql(sourceBuilder.Build(), builder => builder.MigrationsAssembly(migrationAssemblyName));
        options.EnableSensitiveDataLogging();
    }
}
