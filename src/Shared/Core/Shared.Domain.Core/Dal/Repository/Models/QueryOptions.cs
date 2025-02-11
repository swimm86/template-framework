// ----------------------------------------------------------------------------------------------
// <copyright file="QueryOptions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Dal.Specification.Interfaces;
using Shared.Domain.Core.Dal.Specification.Models;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.Repository.Models;

/// <summary>
/// Настройки запроса для операций с сущностями.
/// </summary>
/// <typeparam name="TEntity">Тип сущности, для которой реализована спецификация.</typeparam>
public class QueryOptions<TEntity>(
    bool withTracking = false,
    bool asSplitQuery = false)
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
    public List<IncludeNode> Includes { get; private set; } = [];

    /// <summary>
    /// Признак необходимости отслеживания изменений сущностей.
    /// </summary>
    public bool WithTracking { get; set; } = withTracking;

    /// <summary>
    /// Признак необходимости отслеживания изменений сущностей.
    /// </summary>
    public bool AsSplitQuery { get; set; } = asSplitQuery;

    /// <summary>
    /// Перегрузка AddInclude для IEnumerable.
    /// </summary>
    /// <typeparam name="TProperty">Параметр навигационного свойства.</typeparam>
    /// <param name="expression">Навигационное свойство.</param>
    /// <returns>Следующий экземпляр IIncludable.</returns>
    public IIncludable<TEntity, TEntity, TProperty> AddInclude<TProperty>(
       Expression<Func<TEntity, IEnumerable<TProperty>>> expression)
    {
        Includes.Add(new IncludeNode(expression, typeof(TEntity), typeof(IEnumerable<TProperty>)));
        return new Includable<TEntity, TEntity, TProperty>(Includes);
    }

    /// <summary>
    /// Перегрузка AddInclude для плоского свойства.
    /// </summary>
    /// <param name="expression">Навигационное свойство.</param>
    /// <typeparam name="TProperty">Тип навигационного свойства.</typeparam>
    /// <returns><see cref="IIncludable{TProperty}"/>Следующий экземпляр IIncludable.</returns>
    public IIncludable<TEntity, TEntity, TProperty> AddInclude<TProperty>(
        Expression<Func<TEntity, TProperty>> expression)
        where TProperty : IEntity
    {
        Includes.Add(new IncludeNode(expression, typeof(TEntity), typeof(TProperty)));
        return new Includable<TEntity, TEntity, TProperty>(Includes);
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
}
