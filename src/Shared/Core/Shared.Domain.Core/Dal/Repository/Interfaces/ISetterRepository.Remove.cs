// ----------------------------------------------------------------------------------------------
// <copyright file="ISetterRepository.Remove.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.Specification.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.Repository.Interfaces;

/// <inheritdoc cref="ISetterRepository{TEntity}"/>
public partial interface ISetterRepository<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// Асинхронно удаляет экземпляр сущности из БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entity">Экземпляр сущности.</param>
    /// <param name="userId">Id пользователя, удалившего запись.</param>
    /// <param name="hard">Признак того, что сущность должна быть удалена физически.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task RemoveAsync(
        TEntity entity,
        Guid? userId,
        bool hard = false,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="RemoveAsync(TEntity, Guid?, bool, CancellationToken)"/>
    /// <param name="entity"/><param name="hard"/><param name="cancellationToken"/>
    Task RemoveAsync(
        TEntity entity,
        bool hard = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно удаляет коллекцию экземпляров сущности из БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entities">Коллекция экземпляров сущности.</param>
    /// <param name="hard">Признак того, что сущность должна быть удалена физически.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task RemoveRangeAsync(
        IEnumerable<TEntity> entities,
        bool hard = false,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="RemoveRangeAsync(QueryOptions{TEntity}, bool, CancellationToken)"/>
    /// <param name="specification">Спецификация для отбора удаляемых сущностей.</param>
    /// <param name="hard"/><param name="cancellationToken"/>
    Task RemoveRangeAsync(
        ISpecification<TEntity> specification,
        bool hard = false,
        CancellationToken cancellationToken = default) =>
        RemoveRangeAsync(specification.BuildOptions(), hard, cancellationToken);

    /// <inheritdoc cref="RemoveRangeAsync(IEnumerable{TEntity}, bool, CancellationToken)"/>
    /// <param name="entities"/><param name="cancellationToken"/>
    /// <remarks>Сущности удаляются физически, мягкое удаление не применяется.</remarks>
    Task RemovePermanentRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно удаляет сущности, отобранные с учётом настроек запроса.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <param name="hard">Удалить сущности физически (true) или применить мягкое удаление (false).</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача выполнения операции удаления.</returns>
    Task RemoveRangeAsync(
        QueryOptions<TEntity> options,
        bool hard = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно удаляет сущности, удовлетворяющие заданному условию.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="predicate">Выражение-условие для отбора удаляемых сущностей.</param>
    /// <param name="hard">Удалить сущности физически (true) или применить мягкое удаление (false).</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача выполнения операции удаления.</returns>
    Task RemoveRangeAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool hard = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно физически удаляет коллекцию экземпляров сущности напрямую из БД (без предварительной загрузки экземпляров) по указанной настройке.
    /// </summary>
    /// <remarks>
    /// Имеет эффект без вызова <see cref="SaveChangesAsync"/>.
    /// </remarks>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    Task ExecuteRemoveRangeAsync(
        QueryOptions<TEntity> options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно физически удаляет коллекцию экземпляров сущности напрямую из БД (без предварительной загрузки экземпляров).
    /// </summary>
    /// <inheritdoc cref="ExecuteRemoveRangeAsync(QueryOptions{TEntity}, CancellationToken)"/>
    /// <param name="predicate">Условие выборки для последующего удаления.</param>
    /// <param name="cancellationToken"/>
    Task ExecuteRemoveRangeAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        ExecuteRemoveRangeAsync(new QueryOptions<TEntity>().AddFilter(predicate), cancellationToken);

    /// <inheritdoc cref="ExecuteRemoveRangeAsync(QueryOptions{TEntity}, CancellationToken)"/>
    /// <param name="specification">Спецификация.</param>
    /// <param name="cancellationToken"/>
    Task ExecuteRemoveRangeAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        ExecuteRemoveRangeAsync(specification.BuildOptions(), cancellationToken);
}
