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
    /// Добавляет критерии выборки в заданный <see cref="IQueryable"/>
    /// </summary>
    /// <param name="queryable">Поставщик запросов для типа сущности <typeparamref name="TEntity"/>.</param>
    /// <param name="options">Настройки запроса. Если null, то запрос будет выполнен без дополнительных настроек.</param>
    /// <typeparam name="TEntity">Тип сущности, для которой будет реализована спецификация.</typeparam>
    IQueryable<TEntity> Build<TEntity>(
        IQueryable<TEntity> queryable,
        QueryOptions<TEntity> options) where TEntity : class, IEntity;

    /// <summary>
    /// Добавляет критерии выборки в заданный <see cref="IQueryable"/> и производит преобразование из <typeparamref name="TEntity" /> в <typeparamref name="TOut"/>
    /// </summary>
    /// <typeparam name="TOut">Целевой тип.</typeparam>
    /// <param name="queryable">Поставщик запросов для типа сущности <see cref="TEntity"/>>.</param>
    /// <param name="options">Настройки запроса. Если null, то запрос будет выполнен без дополнительных настроек.</param>
    /// <typeparam name="TEntity">Тип сущности, для которой будет реализована спецификация.</typeparam>
    IQueryable<TOut> BuildWithTransform<TEntity, TOut>(
        IQueryable<TEntity> queryable,
        QueryOptions<TEntity> options) where TEntity : class, IEntity;
}
