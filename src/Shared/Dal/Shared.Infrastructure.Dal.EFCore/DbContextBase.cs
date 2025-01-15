// ----------------------------------------------------------------------------------------------
// <copyright file="DbContextBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Shared.Common.Extensions;
using Shared.Domain.Core.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Conventions;

namespace Shared.Infrastructure.Dal.EFCore;

/// <summary>
/// Базовый класс для <see cref="DbContext"/>.
/// </summary>
public abstract class DbContextBase(
    DbContextOptions options,
    IHostEnvironment environment)
    : DbContext(options)
{
    /// <inheritdoc/>
    public sealed override int SaveChanges()
    {
        ProcessWithOnSavingEntitiesAsync().GetAwaiter().GetResult();
        BeforeSaveActionAsync().GetAwaiter().GetResult();
        return base.SaveChanges();
    }

    /// <inheritdoc/>
    public sealed override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await ProcessWithOnSavingEntitiesAsync(cancellationToken);
        await BeforeSaveActionAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Действия перед сохранением.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    protected virtual Task BeforeSaveActionAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

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
        var cultureInfo = new CultureInfo("en-US") { DateTimeFormat = { ShortDatePattern = "dd/MM/yyyy" } };

        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
    }

    private Task ProcessWithOnSavingEntitiesAsync(CancellationToken cancellationToken = default)
    {
        return ChangeTracker.Entries<IWithOnSavingAction>()
            .ForeachAsync(async x =>
            {
                await x.Entity.OnSavingAsync(cancellationToken);
                x.Entity.IsOnSavingConfirmed = false;
            });
    }
}
