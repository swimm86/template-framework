// ----------------------------------------------------------------------------------------------
// <copyright file="EfUnitOfWork.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Repository;

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
    /// <inheritdoc cref="IQueryEvaluator"/>.
    /// </summary>
    private readonly IQueryEvaluator _evaluator;

    /// <summary>
    /// Конструктор по умолчанию.
    /// </summary>
    /// <param name="dbContextFactory"><see cref="IDbContextFactory{TDbContext}"/>.</param>
    /// <param name="evaluator"><see cref="IQueryEvaluator"/>.</param>
    public EfUnitOfWork(
        IDbContextFactory<TDbContext> dbContextFactory,
        IQueryEvaluator evaluator)
    {
        DbContext = dbContextFactory.CreateDbContext();
        _evaluator = evaluator;
    }

    /// <inheritdoc />
    public TResult Execute<TEntity, TResult>(
        Func<IRepository<TEntity>, TResult> process,
        bool useTransaction = false)
        where TEntity : class, IEntity
    {
        var repository = GetRepository<TEntity>();
        return repository.Execute(() => process(repository), useTransaction);
    }

    /// <inheritdoc />
    public Task<TResult> ExecuteAsync<TEntity, TResult>(
        Func<IRepository<TEntity>, Task<TResult>> process,
        CancellationToken token,
        bool useTransaction = false)
        where TEntity : class, IEntity
    {
        var repository = GetRepository<TEntity>();
        return repository.ExecuteAsync(() => process(repository), token, useTransaction);
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

    /// <summary>
    /// Возвращает репозиторий с сущностями типа <typeparamref name="TEntity"/>.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности, для которого создается репозиторий.</typeparam>
    /// <returns>Репозиторий с сущностями типа <typeparamref name="TEntity"/>.</returns>
    public IRepository<TEntity> GetRepository<TEntity>()
        where TEntity : class, IEntity =>
        new EfRepository<TEntity>(DbContext, _evaluator);

    /// <inheritdoc />
    public void Dispose()
    {
        DbContext.Dispose();
    }
}
