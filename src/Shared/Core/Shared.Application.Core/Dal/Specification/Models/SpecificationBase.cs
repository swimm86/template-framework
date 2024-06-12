// ----------------------------------------------------------------------------------------------
// <copyright file="SpecificationBase.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Application.Core.Dal.Repository.Models;
using Shared.Application.Core.Dal.Specification.Interfaces;
using Shared.Common.Extensions;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Dal.Specification.Models;

/// <summary>
/// Базовый класс для спецификаций.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public abstract record SpecificationBase<TEntity> : ISpecification<TEntity>
    where TEntity : class, IEntity
{
    private readonly QueryOptions<TEntity> _options = new QueryOptions<TEntity>();

    /// <inheritdoc />
    public abstract QueryOptions<TEntity> BuildOptions();

    /// <summary>
    /// Добавляет свойство навигации для включения в запрос к коллекции.
    /// </summary>
    /// <typeparam name="TProperty">Тип свойства навигации.</typeparam>
    /// <param name="expression">Выражение для доступа к свойству навигации.</param>
    /// <returns>Объект, позволяющий добавить дополнительные включения.</returns>
    protected IIncludable<TProperty> AddInclude<TProperty>(Expression<Func<TEntity, ICollection<TProperty>>> expression)
    {
        var includable = new Includable<TProperty>(_options.Includes);
        includable.AddInclude(expression.GetPropertyName());

        return includable;
    }

    /// <summary>
    /// Добавляет свойство навигации для включения в запрос.
    /// </summary>
    /// <typeparam name="TProperty">Тип свойства навигации.</typeparam>
    /// <param name="include">Выражение для доступа к свойству навигации.</param>
    /// <returns>Объект, позволяющий добавить дополнительные включения.</returns>
    protected IIncludable<TProperty> AddInclude<TProperty>(Expression<Func<TEntity, TProperty>> include)
    {
        var includable = new Includable<TProperty>(_options.Includes);
        includable.AddInclude(include.GetPropertyName());

        return includable;
    }

    /// <summary>
    /// Добавляет выражение фильтрации к запросу.
    /// </summary>
    /// <param name="expression">Выражение, определяющее условие фильтрации.</param>
    protected void AddFilter(Expression<Func<TEntity, bool>> expression)
    {
        _options.Filters.Add(expression);
    }

    /// <summary>
    /// Добавляет выражение сортировки к запросу.
    /// </summary>
    /// <param name="expression">Выражение, определяющее ключ сортировки.</param>
    /// <param name="orderDirectionType">Направление сортировки (возрастание или убывание).</param>
    protected void AddOrderBy(Expression<Func<TEntity, object>> expression, OrderDirectionType orderDirectionType)
    {
        _options.OrderBy.Add(new QueryOrderByOption<TEntity>(expression, orderDirectionType));
    }
}
