// ----------------------------------------------------------------------------------------------
// <copyright file="QueryOptions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Application.Core.Dal.Specification.Interfaces;
using Shared.Application.Core.Dal.Specification.Models;
using Shared.Common.Extensions;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Dal.Repository.Models;

/// <summary>
/// Настройки запроса для операций с сущностями.
/// </summary>
/// <typeparam name="TEntity">Тип сущности, для которой реализована спецификация.</typeparam>
public class QueryOptions<TEntity>
    where TEntity : IEntity
{
    /// <summary>
    /// Фильтры.
    /// </summary>
    public List<Expression<Func<TEntity, bool>>> Filters { get; private set; } = [];

    /// <summary>
    /// Настройки сортировки.
    /// </summary>
    public List<QueryOrderByOption<TEntity>> OrderBy { get; private set; } = [];

    /// <summary>
    /// Включаемые связанные сущности.
    /// </summary>
    public List<string> Includes { get; private set; } = [];

    /// <summary>
    /// Признак необходимости отслеживания изменений сущностей.
    /// </summary>
    public bool WithTracking { get; set; }

    /// <summary>
    /// Include если возвращается коллекция.
    /// </summary>
    /// <param name="expression">Include.</param>
    /// /// <typeparam name="TProperty">Тип навигационного свойства.</typeparam>
    /// <returns><see cref="IIncludable{TProperty}"/>.</returns>
    public IIncludable<TProperty> AddInclude<TProperty>(
        Expression<Func<TEntity, ICollection<TProperty>>> expression)
    {
        var includable = new Includable<TProperty>(Includes);
        includable.AddInclude(expression.GetPropertyName());
        return includable;
    }

    /// <summary>
    /// Include.
    /// </summary>
    /// <param name="include">Include.</param>
    /// <typeparam name="TProperty">Тип навигационного свойства.</typeparam>
    /// <returns><see cref="IIncludable{TProperty}"/>.</returns>
    public IIncludable<TProperty> AddInclude<TProperty>(
        Expression<Func<TEntity, TProperty>> include)
    {
        var includable = new Includable<TProperty>(Includes);
        includable.AddInclude(include.GetPropertyName());
        return includable;
    }

    /// <summary>
    /// Добавление фильтра.
    /// </summary>
    /// <param name="expression">Фильтр.</param>
    public void AddFilter(
        Expression<Func<TEntity, bool>> expression)
    {
        Filters.Add(expression);
    }

    /// <summary>
    /// Добавление сортировки.
    /// </summary>
    /// <param name="expression">Сортировка.</param>
    /// <param name="orderDirectionType">Направление сортировки.</param>
    public void AddOrderBy(
        Expression<Func<TEntity, object>> expression,
        OrderDirectionType orderDirectionType)
    {
        OrderBy.Add(new QueryOrderByOption<TEntity>(expression, orderDirectionType));
    }
}
