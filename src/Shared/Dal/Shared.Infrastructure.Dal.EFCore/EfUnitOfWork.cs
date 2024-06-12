// ----------------------------------------------------------------------------------------------
// <copyright file="EfUnitOfWork.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore;

/// <inheritdoc />
public class EfUnitOfWork<TDbContext> : IUnitOfWork
    where TDbContext : DbContextBase
{
    /// <summary>
    /// DbContext.
    /// </summary>
    protected readonly TDbContext DbContext;

    /// <summary>
    /// Конструктор по умолчанию.
    /// </summary>
    /// <param name="dbContextFactory"><see cref="IDbContextFactory{TDbContext}"/>.</param>
    public EfUnitOfWork(IDbContextFactory<TDbContext> dbContextFactory)
    {
        DbContext = dbContextFactory.CreateDbContext();
    }

    /// <inheritdoc />
    public TResult Execute<TEntity, TResult>(
        Func<IRepository<TEntity>, TResult> process,
        bool useTransaction = false)
        where TEntity : class, IEntity
    {
        return DbContext.Execute(process, useTransaction);
    }

    /// <inheritdoc />
    public Task<TResult> ExecuteAsync<TEntity, TResult>(
        Func<IRepository<TEntity>, Task<TResult>> process,
        CancellationToken token,
        bool useTransaction = false)
        where TEntity : class, IEntity
    {
        return DbContext.ExecuteAsync(process, token, useTransaction);
    }

    /// <inheritdoc />
    public int SaveChanges()
    {
        return DbContext.SaveChanges();
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken token = default)
    {
        return DbContext.SaveChangesAsync(token);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DbContext.Dispose();
    }
}
