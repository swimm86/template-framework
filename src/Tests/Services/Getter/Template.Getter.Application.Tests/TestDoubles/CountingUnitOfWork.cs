// ----------------------------------------------------------------------------------------------
// <copyright file="CountingUnitOfWork.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Interfaces;
using Shared.Testing.Doubles.Repository;

namespace Template.Getter.Application.Tests.TestDoubles;

/// <summary>
/// <see cref="IUnitOfWork"/>, считающий количество вызовов
/// <c>GetRepository&lt;T&gt;()</c>. Делегирует остальные операции
/// внутреннему <see cref="FakeUnitOfWork"/>.
/// </summary>
internal sealed class CountingUnitOfWork : IUnitOfWork
{
    private readonly FakeUnitOfWork _inner = new();

    /// <summary>
    /// Количество вызовов <c>GetRepository&lt;T&gt;()</c>.
    /// </summary>
    public int GetRepositoryCallCount { get; private set; }

    /// <summary>
    /// Возвращает <see cref="FakeRepository{TEntity}"/> через внутренний
    /// <see cref="FakeUnitOfWork"/>. Не увеличивает счётчик.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности.</typeparam>
    /// <returns>Экземпляр <see cref="FakeRepository{TEntity}"/>.</returns>
    public FakeRepository<TEntity> GetOrCreateRepository<TEntity>()
        where TEntity : class, IEntity =>
        _inner.GetOrCreateRepository<TEntity>();

    /// <summary>
    /// Возвращает репозиторий <typeparamref name="TEntity"/> через внутренний
    /// <see cref="FakeUnitOfWork"/>, увеличивая счётчик вызовов.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности.</typeparam>
    /// <returns>Репозиторий <typeparamref name="TEntity"/>.</returns>
    public IRepository<TEntity> GetRepository<TEntity>()
        where TEntity : class, IEntity
    {
        GetRepositoryCallCount++;
        return _inner.GetRepository<TEntity>();
    }

    /// <inheritdoc />
    public int SaveChanges(
        bool commitTransaction = true,
        bool resetLifecycleActionSettingsAfterSave = true) =>
        _inner.SaveChanges(commitTransaction, resetLifecycleActionSettingsAfterSave);

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default,
        bool commitTransaction = true,
        bool resetLifecycleActionSettingsAfterSave = true) =>
        _inner.SaveChangesAsync(cancellationToken, commitTransaction, resetLifecycleActionSettingsAfterSave);

    /// <inheritdoc />
    public Task CommitTransactionAsync(CancellationToken cancellationToken) =>
        _inner.CommitTransactionAsync(cancellationToken);

    /// <inheritdoc />
    public Task RollbackTransactionAsync(CancellationToken cancellationToken) =>
        _inner.RollbackTransactionAsync(cancellationToken);

    /// <inheritdoc />
    public Task ResetTransactionAsync(CancellationToken cancellationToken) =>
        _inner.ResetTransactionAsync(cancellationToken);

    /// <inheritdoc />
    public IUnitOfWork EnableTransaction() => this;

    /// <inheritdoc />
    public IUnitOfWork DisableTransaction() => this;

    /// <inheritdoc />
    public void ClearTracking() => _inner.ClearTracking();

    /// <inheritdoc />
    public void Dispose() => _inner.Dispose();
}
