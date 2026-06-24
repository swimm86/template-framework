// ----------------------------------------------------------------------------------------------
// <copyright file="DbContextOptionsBuilderInitializer.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
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
        var settings = configuration.GetOptions<TSettings>();
        var connectionString = settings?.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string for {typeof(TSettings).Name} is not configured.");
        }

        var sourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        options.UseNpgsql(sourceBuilder.Build(), builder => builder.MigrationsAssembly(migrationAssemblyName));

        if (settings?.EnableSensitiveDataLogging is true)
        {
            options.EnableSensitiveDataLogging();
        }
    }
}
