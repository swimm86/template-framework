// ----------------------------------------------------------------------------------------------
// <copyright file="IQueryEvaluator.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.Repository.Models;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Dal.Repository.Interfaces;

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
    /// <returns>Запрос <see cref="IQueryable"/> с примененными критериями выборки и преобразованными элементами типа <typeparamref name="TOut"/>.</returns>
    IQueryable<TOut> BuildWithTransform<TEntity, TOut>(
        IQueryable<TEntity> queryable,
        QueryOptions<TEntity>? options = null)
        where TEntity : class, IEntity;
}
