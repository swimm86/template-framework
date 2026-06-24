// ----------------------------------------------------------------------------------------------
// <copyright file="DbUpdaterBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.DbUpdater.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore;

/// <summary>
/// Реализация <see cref="IDbUpdater"/>.
/// </summary>
/// <param name="dbContext"><see cref="DbContext"/>.</param>
/// <param name="ensureSchemaStrategy">Стратегия инициализации схемы базы данных.</param>
public abstract class DbUpdaterBase(
    DbContext dbContext,
    IEnsureSchemaStrategy ensureSchemaStrategy)
    : IDbUpdater, IDisposable, IAsyncDisposable
{
    /// <inheritdoc />
    public void CreateDbIfNotExists()
    {
        ensureSchemaStrategy.EnsureSchemaIfNeeded();
    }

    /// <inheritdoc />
    public virtual void Migrate()
    {
        if (dbContext.Database.GetPendingMigrations().Any())
        {
            dbContext.Database.Migrate();
        }
    }

    /// <inheritdoc />
    public virtual void Initialize()
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
        dbContext.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return dbContext.DisposeAsync();
    }
}
