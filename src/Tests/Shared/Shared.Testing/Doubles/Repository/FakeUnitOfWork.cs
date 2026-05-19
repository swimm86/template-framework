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
    public int EnableEventsCallCount { get; private set; }
    public int DisableEventsCallCount { get; private set; }
    public int ResetEventSettingsCallCount { get; private set; }

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

    public int SaveChanges(bool commitTransaction = true, bool resetEventSettingsAfterSave = true)
    {
        SaveChangesCallCount++;
        return 0;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default, bool commitTransaction = true, bool resetEventSettingsAfterSave = true)
    {
        SaveChangesAsyncCallCount++;
        LastSaveChangesCancellationToken = cancellationToken;
        return await Task.FromResult(0);
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

    public IUnitOfWork DisableEvents()
    {
        DisableEventsCallCount++;
        return this;
    }

    public IUnitOfWork EnableEvents()
    {
        EnableEventsCallCount++;
        return this;
    }

    public IUnitOfWork DisableEvents<TEntity>(DomainEventType? eventType = default)
        where TEntity : IEntity, IWithDomainEvents
    {
        DisableEventsCallCount++;
        return this;
    }

    public IUnitOfWork EnableEvents<TEntity>(DomainEventType? eventType = default)
        where TEntity : IEntity, IWithDomainEvents
    {
        EnableEventsCallCount++;
        return this;
    }

    public IUnitOfWork DisableEvents<TEntity>(DomainEventType eventType, Enum eventKeyFlags)
        where TEntity : IEntity, IWithDomainEvents
    {
        DisableEventsCallCount++;
        return this;
    }

    public IUnitOfWork EnableEvents<TEntity>(DomainEventType eventType, Enum eventKeyFlags)
        where TEntity : IEntity, IWithDomainEvents
    {
        EnableEventsCallCount++;
        return this;
    }

    public IUnitOfWork ResetEventSettings()
    {
        ResetEventSettingsCallCount++;
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
