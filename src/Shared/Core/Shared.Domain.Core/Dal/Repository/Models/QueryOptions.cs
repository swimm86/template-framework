// ----------------------------------------------------------------------------------------------
// <copyright file="QueryOptions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Common.Extensions;
using Shared.Common.Helpers;
using Shared.Domain.Core.Dal.Models;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.Repository.Models;

/// <summary>
/// Настройки запроса для операций с сущностями.
/// </summary>
/// <typeparam name="TEntity">Тип сущности, для которой реализована спецификация.</typeparam>
public class QueryOptions<TEntity>(
    bool withTracking = false,
    bool asSplitQuery = false,
    bool distinct = false)
    where TEntity : IEntity
{
    private readonly HashSet<string> _orderByFields = [];

    /// <summary>
    /// Пользовательское пост-преобразование IQueryable{TEntity}.
    /// </summary>
    public List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> CustomQueryPostProcesses { get; set; } = [];

    /// <summary>
    /// Пользовательское пре-преобразование IQueryable{TEntity}.
    /// </summary>
    public List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> CustomQueryBeforeProcesses { get; set; } = [];

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
    public List<IIncludable<TEntity>> Includes { get; private set; } = [];

    /// <summary>
    /// Признак необходимости отслеживания изменений сущностей.
    /// </summary>
    public bool WithTracking { get; set; } = withTracking;

    /// <summary>
    /// Признак необходимости отслеживания изменений сущностей.
    /// </summary>
    public bool AsSplitQuery { get; set; } = asSplitQuery;

    /// <summary>
    /// Признак исключения дублей.
    /// </summary>
    public bool Distinct { get; set; } = distinct;

    /// <summary>
    /// Условие для исключения дублей.
    /// </summary>
    public Expression<Func<TEntity, bool>>? DistinctBy { get; set; }

    /// <summary>
    /// Include для плоских свойств.
    /// </summary>
    /// <param name="expression">Выражение инклюда.</param>
    /// <typeparam name="TProperty">Тип навигационного свойства.</typeparam>
    /// <returns><see cref="IIncludable{TProperty}"/>.</returns>
    public Includable<TEntity, TProperty> AddInclude<TProperty>(
        Expression<Func<TEntity, TProperty>> expression)
    {
        var includable = new Includable<TEntity, TProperty>(expression);
        Includes.Add(includable);
        return includable;
    }

    /// <summary>
    /// Inlcude для типа коллекций.
    /// </summary>
    /// <param name="expression">Выражение инклюда.</param>
    /// <typeparam name="TProperty">Тип навигационного свойства.</typeparam>
    /// <returns><see cref="IIncludable{TProperty}"/>.</returns>
    public Includable<TEntity, TProperty> AddInclude<TProperty>(
        Expression<Func<TEntity, IEnumerable<TProperty>>> expression)
    {
        var includable = new Includable<TEntity, TProperty>(expression);
        Includes.Add(includable);
        return includable;
    }

    /// <summary>
    /// Добавление фильтра.
    /// </summary>
    /// <param name="expression">Фильтр.</param>
    /// <returns><see cref="QueryOptions{TEntity}"/>.</returns>
    public QueryOptions<TEntity> AddFilter(
        Expression<Func<TEntity, bool>> expression)
    {
        Filters.Add(expression);
        return this;
    }

    /// <summary>
    /// Добавление фильтра при условии.
    /// </summary>
    /// <param name="condition">Условие для добавления фильтра.</param>
    /// <param name="expression">Фильтр.</param>
    /// <returns><see cref="QueryOptions{TEntity}"/>.</returns>
    public QueryOptions<TEntity> AddFilterIf(
        bool condition,
        Expression<Func<TEntity, bool>> expression)
    {
        if (condition)
        {
            AddFilter(expression);
        }

        return this;
    }

    /// <summary>
    /// Добавление сортировки.
    /// </summary>
    /// <param name="expression">Сортировка.</param>
    /// <param name="orderDirectionType">Направление сортировки.</param>
    /// <param name="index">Индекс.</param>
    /// <returns><see cref="QueryOptions{TEntity}"/>.</returns>
    public QueryOptions<TEntity> AddOrderBy(
        Expression<Func<TEntity, object>> expression,
        OrderDirectionType orderDirectionType,
        int? index = default)
    {
        if (OrderBy.Any(e => e.Expression.Equals(expression)))
        {
            return this;
        }

        var newOrderBy = new QueryOrderByOption<TEntity>(expression, orderDirectionType);
        if (index.HasValue)
        {
            OrderBy.Insert(index.Value, newOrderBy);
        }
        else
        {
            OrderBy.Add(new QueryOrderByOption<TEntity>(expression, orderDirectionType));
        }

        return this;
    }

    /// <summary>
    /// Добавление сортировки.
    /// </summary>
    /// <param name="sortOption">Модель сортировки.</param>
    public void AddOrderBy(
        SortOption sortOption)
    {
        var propToSort = sortOption.Key.ToCamelCase();
        if (!_orderByFields.Add(propToSort))
        {
            return;
        }

        if (!ApplySorting(propToSort, sortOption.DirectionType))
            ApplySorting(propToSort, sortOption.DirectionType);
    }

    /// <summary>
    /// Добавление сортировки при условии.
    /// </summary>
    /// <param name="condition">Условие для добавления сортировки.</param>
    /// <param name="expression">Сортировка.</param>
    /// <param name="orderDirectionType">Направление сортировки.</param>
    /// <param name="index">Индекс.</param>
    /// <returns><see cref="QueryOptions{TEntity}"/>.</returns>
    public QueryOptions<TEntity> AddOrderByIf(
        bool condition,
        Expression<Func<TEntity, object>> expression,
        OrderDirectionType orderDirectionType,
        int? index = default)
    {
        if (condition)
        {
            AddOrderBy(expression, orderDirectionType, index);
        }

        return this;
    }

    private bool ApplySorting(
        string propToSort,
        OrderDirectionType directionType)
    {
        var prop = typeof(TEntity).GetProperties()
            .FirstOrDefault(p => p.Name.Equals(propToSort, StringComparison.OrdinalIgnoreCase));
        if (prop is null)
        {
            return false;
        }

        if (prop.PropertyType == typeof(bool))
        {
            directionType = directionType == OrderDirectionType.Ascending
                ? OrderDirectionType.Descending
                : OrderDirectionType.Ascending;
        }

        var expression = ExpressionHelper.GetPropExpression<TEntity>(prop.Name);
        AddOrderBy(expression, directionType);
        return true;
    }
}
