// ----------------------------------------------------------------------------------------------
// <copyright file="DbContextOptionsBuilderInitializer.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
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
    private const string DefaultConnectionString = "Host=localhost:5433;Database=template;Username=postgres;Password=sw;commandtimeout=0;Include Error Detail=true;Search Path=public";

    /// <inheritdoc />
    public void Initialize<TSettings>(
        DbContextOptionsBuilder options,
        string migrationAssemblyName)
        where TSettings : DbSettingsBase
    {
        var settings = configuration.GetOptions<TSettings>();
        var connectionString = string.IsNullOrWhiteSpace(settings?.ConnectionString)
            ? DefaultConnectionString
            : settings.ConnectionString;
        var sourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        options.UseNpgsql(sourceBuilder.Build(), builder => builder.MigrationsAssembly(migrationAssemblyName));
        options.EnableSensitiveDataLogging();
    }
}
