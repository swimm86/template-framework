// ----------------------------------------------------------------------------------------------
// <copyright file="SpecificationBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.Specification.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.Specification.Models;

/// <summary>
/// Базовый класс для спецификаций.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public abstract record SpecificationBase<TEntity> : ISpecification<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// <see cref="QueryOptions{TEntity}"/>.
    /// </summary>
    protected readonly QueryOptions<TEntity> Options = new();

    /// <inheritdoc />
    public abstract QueryOptions<TEntity> BuildOptions();

    /// <summary>
    /// Добавляет свойство навигации для включения в запрос к коллекции.
    /// </summary>
    /// <typeparam name="TProperty">Тип свойства навигации.</typeparam>
    /// <param name="expression">Выражение для доступа к свойству навигации.</param>
    /// <returns>Объект, позволяющий добавить дополнительные включения.</returns>
    protected IIncludable<TEntity, TEntity, TProperty> AddInclude<TProperty>(
        Expression<Func<TEntity, IEnumerable<TProperty>>> expression) =>
        Options.AddInclude(expression);

    /// <summary>
    /// Добавляет выражение фильтрации к запросу.
    /// </summary>
    /// <param name="expression">Выражение, определяющее условие фильтрации.</param>
    protected void AddFilter(Expression<Func<TEntity, bool>> expression) =>
        Options.AddFilter(expression);

    /// <summary>
    /// Добавляет выражение сортировки к запросу.
    /// </summary>
    /// <param name="expression">Выражение, определяющее ключ сортировки.</param>
    /// <param name="orderDirectionType">Направление сортировки (возрастание или убывание).</param>
    protected void AddOrderBy(
        Expression<Func<TEntity, object>> expression,
        OrderDirectionType orderDirectionType) =>
        Options.AddOrderBy(expression, orderDirectionType);
}
