// ----------------------------------------------------------------------------------------------
// <copyright file="IQueryEvaluator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.Repository.Interfaces;

/// <summary>
/// Интерфейс для сборщика запросов с настройками <see cref="QueryOptions{TEntity}"/>.
/// </summary>
public interface IQueryEvaluator
{
    /// <summary>
    /// Добавляет критерии выборки к предоставленному <see cref="IQueryable"/> запросу.
    /// </summary>
    /// <param name="queryable">Запрос <see cref="IQueryable"/> для типа сущности <typeparamref name="TEntity"/>.</param>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <typeparam name="TEntity">Тип сущности, для которой будет реализован запрос.</typeparam>
    /// <returns>Запрос <see cref="IQueryable"/> с примененными критериями выборки.</returns>
    IQueryable<TEntity> Build<TEntity>(
        IQueryable<TEntity> queryable,
        QueryOptions<TEntity>? options = null)
        where TEntity : class, IEntity;

    /// <summary>
    /// Добавляет критерии выборки к предоставленному <see cref="IQueryable"/> запросу и осуществляет преобразование типов с <typeparamref name="TEntity" /> на <typeparamref name="TOut"/>.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности, для которой будет реализован запрос.</typeparam>
    /// <typeparam name="TOut">Тип результата, к которому будут преобразованы элементы запроса.</typeparam>
    /// <param name="queryable">Запрос <see cref="IQueryable"/> для типа сущности <typeparamref name="TEntity"/>.</param>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="parameters">Необязательные параметры, используемые при проекции.</param>
    /// <returns>Запрос <see cref="IQueryable"/> с примененными критериями выборки и преобразованными элементами типа <typeparamref name="TOut"/>.</returns>
    IQueryable<TOut> BuildWithTransform<TEntity, TOut>(
        IQueryable<TEntity> queryable,
        QueryOptions<TEntity>? options = null,
        object? parameters = null)
        where TEntity : class, IEntity;

    /// <summary>
    /// Добавляет критерии выборки к предоставленному <see cref="IQueryable"/> запросу и осуществляет преобразование типов с <typeparamref name="TEntity" /> на <typeparamref name="TOut"/>.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности, для которой будет реализован запрос.</typeparam>
    /// <typeparam name="TIntermediate">Промежуточный тип в запросе.</typeparam>
    /// <typeparam name="TOut">Тип результата, к которому будут преобразованы элементы запроса.</typeparam>
    /// <param name="queryable">Запрос <see cref="IQueryable"/> для типа сущности <typeparamref name="TEntity"/>.</param>
    /// <param name="postBuildProcess">Процесс, который необходимо выполнить после применения настроек запроса.</param>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <returns>Запрос <see cref="IQueryable"/> с примененными критериями выборки и преобразованными элементами типа <typeparamref name="TOut"/>.</returns>
    IQueryable<TOut> BuildWithTransform<TEntity, TIntermediate, TOut>(
        IQueryable<TEntity> queryable,
        Func<IQueryable<TEntity>, IQueryable<TIntermediate>> postBuildProcess,
        QueryOptions<TEntity>? options = null)
        where TEntity : class, IEntity;
}
