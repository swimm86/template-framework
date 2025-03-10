// ----------------------------------------------------------------------------------------------
// <copyright file="EfUnitOfWork.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Shared.Common.Extensions;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Repository;

namespace Shared.Infrastructure.Dal.EFCore;

/// <inheritdoc />
public class EfUnitOfWork<TDbContext> : IUnitOfWork
    where TDbContext : DbContextBase
{
    private EntityEntry<IWithDomainEvents>[] EntriesWithDomainEvents =>
        DbContext.ChangeTracker.Entries<IWithDomainEvents>().ToArray();

    /// <summary>
    /// DbContext.
    /// </summary>
    protected readonly TDbContext DbContext;

    /// <summary>
    /// <inheritdoc cref="IQueryEvaluator"/>.
    /// </summary>
    private readonly IQueryEvaluator _evaluator;

    /// <summary>
    /// <inheritdoc cref="IServiceProvider"/>.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// <inheritdoc cref="IBeforeSaveChangesService"/>.
    /// </summary>
    private readonly IBeforeSaveChangesService? _beforeSaveChangesService;

    /// <summary>
    /// Признак того, что необходимо использовать транзакцию.
    /// </summary>
    private bool _useTransaction = true;

    /// <summary>
    /// Конструктор по умолчанию.
    /// </summary>
    /// <param name="dbContextFactory"><see cref="IDbContextFactory{TDbContext}"/>.</param>
    /// <param name="evaluator"><see cref="IQueryEvaluator"/>.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
    /// <param name="beforeSaveChangesService"><see cref="IBeforeSaveChangesService"/>.</param>
    public EfUnitOfWork(
        IDbContextFactory<TDbContext> dbContextFactory,
        IQueryEvaluator evaluator,
        IServiceProvider serviceProvider,
        IBeforeSaveChangesService? beforeSaveChangesService = default)
    {
        DbContext = dbContextFactory.CreateDbContext();
        _evaluator = evaluator;
        _beforeSaveChangesService = beforeSaveChangesService;
        _serviceProvider = serviceProvider;

        if (DbContext.Database.CanConnect())
        {
            EnableTransaction();
        }
    }

    /// <inheritdoc />
    public int SaveChanges(bool commitTransaction = true)
    {
        var entries = EntriesWithDomainEvents;

        try
        {
            ProcessDomainEventsAsync(entries, DomainEventType.BeforeSave).GetAwaiter().GetResult();

            _beforeSaveChangesService?.Process(DbContext);
            var result = DbContext.SaveChanges();
            CommitTransaction(commitTransaction);
            ProcessDomainEventsAsync(entries, DomainEventType.AfterSave).GetAwaiter().GetResult();
            return result;
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
        finally
        {
            entries.ForEach(e => e.Entity.ResetEvents());
            ResetTransaction();
        }
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(
        bool commitTransaction = true,
        CancellationToken token = default)
    {
        var entries = EntriesWithDomainEvents;

        try
        {
            await ProcessDomainEventsAsync(entries, DomainEventType.BeforeSave, token);
            var task = _beforeSaveChangesService?.ProcessAsync(DbContext, token);
            if (task != null)
            {
                await task;
            }

            var result = await DbContext.SaveChangesAsync(token);
            await CommitTransactionAsync(commitTransaction, token);

            await ProcessDomainEventsAsync(entries, DomainEventType.AfterSave, token);
            return result;
        }
        catch
        {
            await RollbackTransactionAsync(token);
            throw;
        }
        finally
        {
            entries.ForEach(e => e.Entity.ResetEvents());
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

    private Task ProcessDomainEventsAsync(
        EntityEntry<IWithDomainEvents>[] entries,
        DomainEventType eventType,
        CancellationToken cancellationToken = default)
    {
        return entries.ForeachAsync(
            async x =>
            {
                await x.Navigations
                    .Where(nav =>
                        x.Entity.RequiredToSaveNavigationPropertiesNames.Contains(nav.Metadata.Name) && !nav.IsLoaded)
                    .ForeachAsync(
                        nav => nav.LoadAsync(cancellationToken),
                        cancellationToken);
                await x.Entity.ProcessDomainEventsAsync(_serviceProvider, eventType, cancellationToken);
            },
            cancellationToken);
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
