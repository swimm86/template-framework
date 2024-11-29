// ----------------------------------------------------------------------------------------------
// <copyright file="IUnitOfWork.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.UnitOfWork.Interfaces;

/// <summary>
/// Интерфейс, который используется для unit of work
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Сохраняет изменения
    /// </summary>
    /// <returns>Код результата.</returns>
    int SaveChanges();

    /// <summary>
    /// Асинхронно сохраняет изменения
    /// </summary>
    /// <param name="token">Токен отмены операции.</param>
    /// <returns>Код результата.</returns>
    Task<int> SaveChangesAsync(CancellationToken token = default);

    /// <summary>
    /// Возвращает репозиторий с сущностями типа <typeparamref name="TEntity"/>.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности, для которого создается репозиторий.</typeparam>
    /// <returns>Репозиторий с сущностями типа <typeparamref name="TEntity"/>.</returns>
    IRepository<TEntity> GetRepository<TEntity>()
        where TEntity : class, IEntity;
}
