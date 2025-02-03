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
    /// Признак того, что необходимо использовать транзакцию.
    /// </summary>
    private bool _useTransaction = true;

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

        if (DbContext.Database.CanConnect())
        {
            EnableTransaction();
        }
    }

    /// <inheritdoc />
    public int SaveChanges(bool commitTransaction = true)
    {
        try
        {
            var result = DbContext.SaveChanges();
            CommitTransaction(commitTransaction);
            return result;
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
        finally
        {
            ResetTransaction();
        }
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(
        bool commitTransaction = true,
        CancellationToken token = default)
    {
        try
        {
            var result = await DbContext.SaveChangesAsync(token);
            await CommitTransactionAsync(commitTransaction, token);
            return result;
        }
        catch
        {
            await RollbackTransactionAsync(token);
            throw;
        }
        finally
        {
            await ResetTransactionAsync(token);
        }
    }

    /// <inheritdoc />
    public IRepository<TEntity> GetRepository<TEntity>()
        where TEntity : class, IEntity =>
        new EfRepository<TEntity>(DbContext, _evaluator);

    /// <inheritdoc />
    public void EnableTransaction()
    {
        _useTransaction = true;
        if (DbContext.Database.CurrentTransaction == null)
        {
            DbContext.Database.BeginTransaction();
        }
    }

    /// <inheritdoc />
    public void DisableTransaction()
    {
        _useTransaction = false;
        DbContext.Database.CurrentTransaction?.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            DbContext.Database.CurrentTransaction?.Dispose();
        }
        finally
        {
            DbContext.Dispose();
        }
    }

    private void CommitTransaction(bool commit)
    {
        if (_useTransaction && commit)
        {
            DbContext.Database.CurrentTransaction?.Commit();
        }
    }

    private void RollbackTransaction()
    {
        if (_useTransaction)
        {
            DbContext.Database.CurrentTransaction?.Rollback();
        }
    }

    private void ResetTransaction()
    {
        if (!_useTransaction)
        {
            return;
        }

        DbContext.Database.CurrentTransaction?.Dispose();
        DbContext.Database.BeginTransaction();
    }

    private Task CommitTransactionAsync(bool commit, CancellationToken token)
    {
        return _useTransaction && commit && DbContext.Database.CurrentTransaction != null
            ? DbContext.Database.CurrentTransaction.CommitAsync(token)
            : Task.CompletedTask;
    }

    private Task RollbackTransactionAsync(CancellationToken token)
    {
        return _useTransaction && DbContext.Database.CurrentTransaction != null
            ? DbContext.Database.CurrentTransaction.RollbackAsync(token)
            : Task.CompletedTask;
    }

    private async Task ResetTransactionAsync(CancellationToken token)
    {
        if (_useTransaction)
        {
            if (DbContext.Database.CurrentTransaction != null)
            {
                await DbContext.Database.CurrentTransaction.DisposeAsync();
            }

            await DbContext.Database.BeginTransactionAsync(token);
        }
    }
}
