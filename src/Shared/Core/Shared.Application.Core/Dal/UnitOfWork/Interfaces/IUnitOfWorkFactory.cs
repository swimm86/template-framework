// ----------------------------------------------------------------------------------------------
// <copyright file="IUnitOfWorkFactory.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Dal.UnitOfWork.Interfaces;

/// <summary>
/// Интерфейс, который используется для фабрики для безопасного взаимодействия с <see cref="IUnitOfWork"/>.
/// </summary>
public interface IUnitOfWorkFactory
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
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="useTransaction">Признак того, что операция будет выполнена в рамках транзакции.</param>
    /// <returns>Результат выполнения операции <see cref="TResult"/>.</returns>
    Task<TResult> ExecuteAsync<TEntity, TResult>(
        Func<IRepository<TEntity>, Task<TResult>> process,
        CancellationToken cancellationToken = default,
        bool useTransaction = false)
        where TEntity : class, IEntity;
}
