// ----------------------------------------------------------------------------------------------
// <copyright file="DbSeeder.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Shared.Application.Core.Dal.DbSeeder.Interfaces;

namespace Gpn.Contour.PpsContract.DatabaseUpgrade;

/// <summary>
/// Реализация <see cref="IDbSeeder"/>.
/// </summary>
/// <param name="dbContextFactory">Фабрика DbContext-ов.</param>
public class DbSeeder(IDbContextFactory<DbContext> dbContextFactory)
    : IDbSeeder, IDisposable, IAsyncDisposable
{
    private readonly DbContext _dbContext = dbContextFactory.CreateDbContext();

    /// <inheritdoc />
    public void Migrate()
    {
        if (_dbContext.Database.GetPendingMigrations().Any())
        {
            _dbContext.Database.Migrate();
        }
    }

    /// <inheritdoc />
    public void Initialize()
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _dbContext.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return _dbContext.DisposeAsync();
    }
}
