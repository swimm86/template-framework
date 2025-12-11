// ----------------------------------------------------------------------------------------------
// <copyright file="DbUpdaterBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.DbSeeder.Interfaces;
using Shared.Application.Core.Dal.DbUpdater.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore;

/// <summary>
/// Реализация <see cref="IDbSeeder"/>.
/// </summary>
/// <param name="dbContext"><see cref="DbContext"/>.</param>
public abstract class DbUpdaterBase(
    DbContext dbContext)
    : IDbUpdater, IDisposable, IAsyncDisposable
{
    /// <inheritdoc />
    public void CreateDbIfNotExists()
    {
        if (!dbContext.Database.GetPendingMigrations().Any())
        {
            dbContext.Database.EnsureCreated();
        }
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
