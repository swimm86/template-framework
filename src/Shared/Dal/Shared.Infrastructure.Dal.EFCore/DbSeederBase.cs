// ----------------------------------------------------------------------------------------------
// <copyright file="DbSeederBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Shared.Application.Core.Dal.DbSeeder.Attributes;
using Shared.Application.Core.Dal.DbSeeder.Entities;
using Shared.Application.Core.Dal.DbSeeder.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore;

/// <summary>
/// Реализация <see cref="IDbSeeder"/>.
/// </summary>
/// <typeparam name="TDbContext">Тип DbContext-а.</typeparam>
/// <param name="dbContextFactory">Фабрика <see cref="TDbContext"/>.</param>
/// <param name="unitOfWork"><see cref="IUnitOfWork"/>.</param>
public abstract class DbSeederBase<TDbContext>(
    IDbContextFactory<TDbContext> dbContextFactory,
    IUnitOfWork unitOfWork)
    : IDbSeeder, IDisposable, IAsyncDisposable
    where TDbContext : DbContext
{
    /// <summary>
    /// DbContext.
    /// </summary>
    protected readonly TDbContext DbContext = dbContextFactory.CreateDbContext();

    /// <inheritdoc />
    public virtual void Migrate()
    {
        if (DbContext.Database.GetPendingMigrations().Any())
        {
            DbContext.Database.Migrate();
        }
    }

    /// <inheritdoc />
    public virtual void Initialize()
    {
        var seeds = Assembly
            .GetCallingAssembly()
            .GetTypes()
            .Where(type => typeof(ISeed).IsAssignableFrom(type))
            .Select(type => new { type, attr = type.GetCustomAttribute<SeedAttribute>() })
            .Where(x => x.attr != null && !DbContext.Set<Seed>().Any(seed => seed.Name == x.attr!.Name))
            .OrderBy(x => x.attr!.Order)
            .ToList();
        seeds.ForEach(x =>
        {
            ((ISeed)Activator.CreateInstance(x.type)!).SeedAsync(unitOfWork).GetAwaiter().GetResult();
            DbContext.Set<Seed>().Add(Seed.Create(x.attr!.Name));
        });

        if (seeds.Any())
        {
            DbContext.SaveChanges();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DbContext.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return DbContext.DisposeAsync();
    }
}
