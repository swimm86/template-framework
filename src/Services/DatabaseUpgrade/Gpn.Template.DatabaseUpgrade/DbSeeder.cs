// ----------------------------------------------------------------------------------------------
// <copyright file="DbSeeder.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Shared.Application.Core.Dal.DbSeeder.Interfaces;
using DbContext = Gpn.Template.Infrastructure.Dal.DbContext;

namespace Gpn.Template.DatabaseUpgrade;

/// <summary>
/// Реализация <see cref="IDbSeeder"/>.
/// </summary>
/// <param name="dbContextFactory">Фабрика DbContext-ов.</param>
public class DbSeeder(IDbContextFactory<DbContext> dbContextFactory) : IDbSeeder
{
    private readonly DbContext _dbContext = dbContextFactory.CreateDbContext();

    /// <inheritdoc />
    public void CreateDbIfNotExists()
    {
        _dbContext.Database.EnsureCreated();
    }

    /// <inheritdoc />
    public void Migrate()
    {
        if (_dbContext.Database.GetPendingMigrations().Any())
        {
            _dbContext.Database.Migrate();
        }
    }
}
