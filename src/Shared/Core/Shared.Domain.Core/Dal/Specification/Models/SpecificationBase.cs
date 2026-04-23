// ----------------------------------------------------------------------------------------------
// <copyright file="SpecificationBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
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
public abstract record SpecificationBase<TEntity>(
    ICollection<SortOption>? SortOptions = default)
    : ISpecification<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// <see cref="QueryOptions{TEntity}"/>.
    /// </summary>
    protected readonly QueryOptions<TEntity> Options = new();

    /// <inheritdoc />
    public virtual QueryOptions<TEntity> BuildOptions()
    {
        SortOptions?.ForEach(Options.AddOrderBy);
        return Options;
    }

    /// <summary>
    /// Добавляет свойство навигации для включения в запрос.
    /// </summary>
    /// <typeparam name="TProperty">Тип свойства навигации.</typeparam>
    /// <param name="expression">Выражение для доступа к свойству навигации.</param>
    /// <returns>Объект, позволяющий добавить дополнительные включения.</returns>
    protected Includable<TEntity, TProperty> AddInclude<TProperty>(
        Expression<Func<TEntity, TProperty>> expression) =>
        Options.AddInclude(expression);

    /// <summary>
    /// Добавляет свойство навигации для включения в запрос.
    /// </summary>
    /// <typeparam name="TProperty">Тип свойства навигации.</typeparam>
    /// <param name="expression">Выражение для доступа к свойству навигации.</param>
    /// <returns>Объект, позволяющий добавить дополнительные включения.</returns>
    protected Includable<TEntity, TProperty> AddInclude<TProperty>(
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
