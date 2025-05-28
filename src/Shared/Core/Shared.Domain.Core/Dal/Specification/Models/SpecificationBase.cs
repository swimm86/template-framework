// ----------------------------------------------------------------------------------------------
// <copyright file="SpecificationBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Common.Extensions;
using Shared.Domain.Core.Dal.Models;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.Specification.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.Specification.Models;

/// <summary>
/// Базовый класс для спецификаций.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public abstract record SpecificationBase<TEntity>
    : ISpecification<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// <see cref="QueryOptions{TEntity}"/>.
    /// </summary>
    protected readonly QueryOptions<TEntity> Options = new();

    /// <summary>
    /// Конструктор класса.
    /// </summary>
    /// <param name="options">Настройки запроса.</param>
    /// <param name="sortOptions">Настройки сортировки.</param>
    /// <param name="filterOptions">Настройки фильтрации.</param>
    protected SpecificationBase(
        QueryOptions<TEntity>? options = default,
        ICollection<SortOption>? sortOptions = default,
        ICollection<FilterOption>? filterOptions = default)
    {
        Options = options ?? new QueryOptions<TEntity>();
        filterOptions?.ForEach(filter => Options.AddFilter(filter));
        sortOptions?.ForEach(Options.AddOrderBy);
    }

    /// <inheritdoc />
    public abstract QueryOptions<TEntity> BuildOptions();

    /// <summary>
    /// Добавляет свойство навигации для включения в запрос к коллекции.
    /// </summary>
    /// <typeparam name="TProperty">Тип свойства навигации.</typeparam>
    /// <param name="expression">Выражение для доступа к свойству навигации.</param>
    /// <returns>Объект, позволяющий добавить дополнительные включения.</returns>
    protected IIncludable<TProperty> AddInclude<TProperty>(
        Expression<Func<TEntity, ICollection<TProperty>>> expression) =>
        Options.AddInclude(expression);

    /// <summary>
    /// Добавляет свойство навигации для включения в запрос.
    /// </summary>
    /// <typeparam name="TProperty">Тип свойства навигации.</typeparam>
    /// <param name="include">Выражение для доступа к свойству навигации.</param>
    /// <returns>Объект, позволяющий добавить дополнительные включения.</returns>
    protected IIncludable<TProperty> AddInclude<TProperty>(
        Expression<Func<TEntity, TProperty>> include) =>
        Options.AddInclude(include);

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
