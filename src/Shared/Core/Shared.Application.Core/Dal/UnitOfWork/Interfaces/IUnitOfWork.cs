// ----------------------------------------------------------------------------------------------
// <copyright file="IUnitOfWork.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Dal.UnitOfWork.Interfaces;

/// <summary>
/// Интерфейс, который используется для unit of work
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Выполняет операцию.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности, для которой будет выполнена операция.</typeparam>
    /// <typeparam name="TResult">Тип результата выполнения операции.</typeparam>
    /// <param name="process">Реализация операции.</param>
    /// <param name="useTransaction">Признак того, что операция будет выполнена в рамках транзакции.</param>
    /// <returns>Результат выполнения операции <see cref="TResult"/>.</returns>
    TResult Execute<TEntity, TResult>(
        Func<IRepository<TEntity>, TResult> process,
        bool useTransaction = false)
        where TEntity : class, IEntity;

    /// <summary>
    /// Выполняет операцию асинхронно.
    /// </summary>
    /// /// <typeparam name="TEntity"> Тип сущности, для которой будет выполнена операция. </typeparam>
    /// <typeparam name="TResult">Тип результата выполнения операции.</typeparam>
    /// <param name="process">Асинхрорнная реализация операции.</param>
    /// <param name="token">Токен отмены операции.</param>
    /// <param name="useTransaction">Признак того, что операция будет выполнена в рамках транзакции.</param>
    /// <returns>Результат выполнения операции <see cref="TResult"/>.</returns>
    Task<TResult> ExecuteAsync<TEntity, TResult>(
        Func<IRepository<TEntity>, Task<TResult>> process,
        CancellationToken token,
        bool useTransaction = false)
        where TEntity : class, IEntity;

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
