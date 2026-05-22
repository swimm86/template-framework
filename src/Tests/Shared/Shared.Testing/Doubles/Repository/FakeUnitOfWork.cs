using System.Collections.Concurrent;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Testing.Doubles.Repository;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    public int SaveChangesCallCount { get; private set; }
    public int SaveChangesAsyncCallCount { get; private set; }
    public int CommitTransactionCallCount { get; private set; }
    public int RollbackTransactionCallCount { get; private set; }
    public int ResetTransactionCallCount { get; private set; }
    public int ClearTrackingCallCount { get; private set; }
    public int EnableTransactionCallCount { get; private set; }
    public int DisableTransactionCallCount { get; private set; }
    public int EnableLifecycleActionsCallCount { get; private set; }
    public int DisableLifecycleActionsCallCount { get; private set; }
    public int ResetLifecycleActionSettingsCallCount { get; private set; }

    public CancellationToken LastSaveChangesCancellationToken { get; private set; }

    public FakeRepository<TEntity> GetOrCreateRepository<TEntity>()
        where TEntity : class, IEntity
    {
        return (FakeRepository<TEntity>)_repositories.GetOrAdd(
            typeof(TEntity),
            _ => new FakeRepository<TEntity>());
    }

    public IRepository<TEntity> GetRepository<TEntity>()
        where TEntity : class, IEntity
        => GetOrCreateRepository<TEntity>();

    public int SaveChanges(
        bool commitTransaction = true,
        bool resetLifecycleActionSettingsAfterSave = true)
    {
        SaveChangesCallCount++;
        return 0;
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default,
        bool commitTransaction = true,
        bool resetLifecycleActionSettingsAfterSave = true)
    {
        SaveChangesAsyncCallCount++;
        LastSaveChangesCancellationToken = cancellationToken;
        return Task.FromResult(0);
    }

    public Task CommitTransactionAsync(CancellationToken cancellationToken)
    {
        CommitTransactionCallCount++;
        return Task.CompletedTask;
    }

    public Task RollbackTransactionAsync(CancellationToken cancellationToken)
    {
        RollbackTransactionCallCount++;
        return Task.CompletedTask;
    }

    public Task ResetTransactionAsync(CancellationToken cancellationToken)
    {
        ResetTransactionCallCount++;
        return Task.CompletedTask;
    }

    public IUnitOfWork EnableTransaction()
    {
        EnableTransactionCallCount++;
        return this;
    }

    public IUnitOfWork DisableTransaction()
    {
        DisableTransactionCallCount++;
        return this;
    }

    public IUnitOfWork DisableLifecycleActions()
    {
        DisableLifecycleActionsCallCount++;
        return this;
    }

    public IUnitOfWork EnableLifecycleActions()
    {
        EnableLifecycleActionsCallCount++;
        return this;
    }

    public IUnitOfWork DisableLifecycleActions<TEntity>
        (LifecycleHookType? hookType = null)
        where TEntity : IEntity, IWithLifecycleActions
    {
        DisableLifecycleActionsCallCount++;
        return this;
    }

    public IUnitOfWork EnableLifecycleActions<TEntity>(
        LifecycleHookType? hookType = null)
        where TEntity : IEntity, IWithLifecycleActions
    {
        EnableLifecycleActionsCallCount++;
        return this;
    }

    public IUnitOfWork DisableLifecycleActions<TEntity>(
        LifecycleHookType hookType,
        Enum actionKeyFlags)
        where TEntity : IEntity, IWithLifecycleActions
    {
        DisableLifecycleActionsCallCount++;
        return this;
    }

    public IUnitOfWork EnableLifecycleActions<TEntity>(
        LifecycleHookType hookType,
        Enum actionKeyFlags)
        where TEntity : IEntity, IWithLifecycleActions
    {
        EnableLifecycleActionsCallCount++;
        return this;
    }

    public IUnitOfWork ResetLifecycleActionSettings()
    {
        ResetLifecycleActionSettingsCallCount++;
        return this;
    }

    public void ClearTracking()
    {
        ClearTrackingCallCount++;
        foreach (var repo in _repositories.Values)
        {
            var clearMethod = repo.GetType().GetMethod("ClearStorage");
            clearMethod?.Invoke(repo, null);
        }
    }

    public void Dispose()
    {
    }
}
