using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;
using Shared.Testing.Doubles.Repository;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed class CallbackUnitOfWork : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = new();
    private readonly Action<CancellationToken>? _onGetRepositoryCalled;

    public CallbackUnitOfWork(Action<CancellationToken>? onGetRepositoryCalled = null)
    {
        _onGetRepositoryCalled = onGetRepositoryCalled;
    }

    public IRepository<TEntity> GetRepository<TEntity>()
        where TEntity : class, IEntity
    {
        if (!_repositories.TryGetValue(typeof(TEntity), out var repo))
        {
            var fakeRepo = new FakeRepository<TEntity>();
            var callbackRepo = new CallbackRepository<TEntity>(fakeRepo, _onGetRepositoryCalled);
            _repositories[typeof(TEntity)] = callbackRepo;
            return callbackRepo;
        }

        return (IRepository<TEntity>)repo;
    }

    public int SaveChanges(bool commitTransaction = true, bool resetLifecycleActionSettingsAfterSave = true) => 0;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default, bool commitTransaction = true, bool resetLifecycleActionSettingsAfterSave = true)
        => Task.FromResult(0);

    public Task CommitTransactionAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task RollbackTransactionAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ResetTransactionAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public IUnitOfWork EnableTransaction() => this;

    public IUnitOfWork DisableTransaction() => this;

    public IUnitOfWork DisableLifecycleActions() => this;

    public IUnitOfWork EnableLifecycleActions() => this;

    public IUnitOfWork DisableLifecycleActions<TEntity>(LifecycleHookType? hookType = default)
        where TEntity : IEntity, IWithLifecycleActions => this;

    public IUnitOfWork EnableLifecycleActions<TEntity>(LifecycleHookType? hookType = default)
        where TEntity : IEntity, IWithLifecycleActions => this;

    public IUnitOfWork DisableLifecycleActions<TEntity>(LifecycleHookType hookType, Enum hookKeyFlags)
        where TEntity : IEntity, IWithLifecycleActions => this;

    public IUnitOfWork EnableLifecycleActions<TEntity>(LifecycleHookType hookType, Enum hookKeyFlags)
        where TEntity : IEntity, IWithLifecycleActions => this;

    public IUnitOfWork ResetLifecycleActionSettings() => this;

    public void ClearTracking()
    {
    }

    public void Dispose()
    {
    }
}
