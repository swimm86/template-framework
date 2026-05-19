using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;
using Shared.Testing.Doubles.Repository;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed class CountingUnitOfWork : IUnitOfWork
{
    public int GetRepositoryCallCount { get; private set; }

    private readonly Dictionary<Type, object> _repositories = new();

    public IRepository<TEntity> GetRepository<TEntity>()
        where TEntity : class, IEntity
    {
        GetRepositoryCallCount++;
        if (!_repositories.TryGetValue(typeof(TEntity), out var repo))
        {
            repo = new FakeRepository<TEntity>();
            _repositories[typeof(TEntity)] = repo;
        }

        return (IRepository<TEntity>)repo;
    }

    public int SaveChanges(bool commitTransaction = true, bool resetEventSettingsAfterSave = true) => 0;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default, bool commitTransaction = true, bool resetEventSettingsAfterSave = true)
        => Task.FromResult(0);

    public Task CommitTransactionAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task RollbackTransactionAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ResetTransactionAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public IUnitOfWork EnableTransaction() => this;

    public IUnitOfWork DisableTransaction() => this;

    public IUnitOfWork DisableEvents() => this;

    public IUnitOfWork EnableEvents() => this;

    public IUnitOfWork DisableEvents<TEntity>(DomainEventType? eventType = default)
        where TEntity : IEntity, IWithDomainEvents => this;

    public IUnitOfWork EnableEvents<TEntity>(DomainEventType? eventType = default)
        where TEntity : IEntity, IWithDomainEvents => this;

    public IUnitOfWork DisableEvents<TEntity>(DomainEventType eventType, Enum eventKeyFlags)
        where TEntity : IEntity, IWithDomainEvents => this;

    public IUnitOfWork EnableEvents<TEntity>(DomainEventType eventType, Enum eventKeyFlags)
        where TEntity : IEntity, IWithDomainEvents => this;

    public IUnitOfWork ResetEventSettings() => this;

    public void ClearTracking()
    {
    }

    public void Dispose()
    {
    }
}
