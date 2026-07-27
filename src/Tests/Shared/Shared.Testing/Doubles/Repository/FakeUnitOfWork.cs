// ----------------------------------------------------------------------------------------------
// <copyright file="FakeUnitOfWork.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Testing.Doubles.Repository;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    public int SaveChangesCallCount => Volatile.Read(ref _saveChangesCallCount);
    public int SaveChangesAsyncCallCount => Volatile.Read(ref _saveChangesAsyncCallCount);
    public int CommitTransactionCallCount => Volatile.Read(ref _commitTransactionCallCount);
    public int RollbackTransactionCallCount => Volatile.Read(ref _rollbackTransactionCallCount);
    public int ResetTransactionCallCount => Volatile.Read(ref _resetTransactionCallCount);
    public int ClearTrackingCallCount => Volatile.Read(ref _clearTrackingCallCount);
    public int EnableTransactionCallCount => Volatile.Read(ref _enableTransactionCallCount);
    public int DisableTransactionCallCount => Volatile.Read(ref _disableTransactionCallCount);

    private int _saveChangesCallCount;
    private int _saveChangesAsyncCallCount;
    private int _commitTransactionCallCount;
    private int _rollbackTransactionCallCount;
    private int _resetTransactionCallCount;
    private int _clearTrackingCallCount;
    private int _enableTransactionCallCount;
    private int _disableTransactionCallCount;

    private CancellationToken _lastSaveChangesCancellationToken;
    private readonly object _lastSaveChangesCancellationTokenLock = new();

    /// <summary>
    /// Последний <see cref="CancellationToken"/>, переданный в <c>SaveChangesAsync</c>.
    /// </summary>
    public CancellationToken LastSaveChangesCancellationToken
    {
        get { lock (_lastSaveChangesCancellationTokenLock) { return _lastSaveChangesCancellationToken; } }
    }

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
        Interlocked.Increment(ref _saveChangesCallCount);
        return 0;
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default,
        bool commitTransaction = true,
        bool resetLifecycleActionSettingsAfterSave = true)
    {
        Interlocked.Increment(ref _saveChangesAsyncCallCount);
        lock (_lastSaveChangesCancellationTokenLock) { _lastSaveChangesCancellationToken = cancellationToken; }
        return Task.FromResult(0);
    }

    public Task CommitTransactionAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _commitTransactionCallCount);
        return Task.CompletedTask;
    }

    public Task RollbackTransactionAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _rollbackTransactionCallCount);
        return Task.CompletedTask;
    }

    public Task ResetTransactionAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _resetTransactionCallCount);
        return Task.CompletedTask;
    }

    public IUnitOfWork EnableTransaction()
    {
        Interlocked.Increment(ref _enableTransactionCallCount);
        return this;
    }

    public IUnitOfWork DisableTransaction()
    {
        Interlocked.Increment(ref _disableTransactionCallCount);
        return this;
    }

    public void ClearTracking()
    {
        Interlocked.Increment(ref _clearTrackingCallCount);
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