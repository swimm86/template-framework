// ----------------------------------------------------------------------------------------------
// <copyright file="DbContextBase.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.Extensions.Hosting;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Conventions;
using Shared.Infrastructure.Dal.EFCore.Repository;

namespace Shared.Infrastructure.Dal.EFCore;

/// <summary>
/// Базовый класс для <see cref="DbContext"/>.
/// </summary>
public abstract class DbContextBase(
    DbContextOptions options,
    IQueryEvaluator evaluator,
    IHostEnvironment environment
) : DbContext(options), IUnitOfWork
{
    #region Implementions

    /// <inheritdoc />
    public TResult Execute<TEntity, TResult>(
        Func<IRepository<TEntity>, TResult> process,
        bool useTransaction = false
    ) where TEntity : class, IEntity
    {
        var repository = new EfRepository<TEntity>(this, evaluator);
        return repository.Execute(() => process(repository), useTransaction);
    }

    /// <inheritdoc />
    public Task<TResult> ExecuteAsync<TEntity, TResult>(
        Func<IRepository<TEntity>, Task<TResult>> process,
        CancellationToken token,
        bool useTransaction = false
    ) where TEntity : class, IEntity
    {
        var repository = new EfRepository<TEntity>(this, evaluator);
        return repository.ExecuteAsync(() => process(repository), token, useTransaction);
    }

    #endregion

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
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

        base.OnConfiguring(optionsBuilder);
    }
}
