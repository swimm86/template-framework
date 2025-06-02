// ----------------------------------------------------------------------------------------------
// <copyright file="QueryOptions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections;
using System.Linq.Expressions;
using System.Text.Json;
using Shared.Common.Extensions;
using Shared.Common.Helpers;
using Shared.Domain.Core.Dal.Models;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Exceptions.Models;
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
    private readonly HashSet<FilterOption> _filtersFields = [];

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
    /// <returns><see cref="QueryOptions{TEntity}"/>.</returns>
    public QueryOptions<TEntity> AddFilter(
        Expression<Func<TEntity, bool>> expression)
    {
        Filters.Add(expression);
        return this;
    }

    /// <summary>
    /// Добавление фильтра.
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns><see cref="QueryOptions{TEntity}"/>.</returns>
    public QueryOptions<TEntity> AddFilter(
        FilterOption filter)
    {
        if (!_filtersFields.Add(filter))
        {
            return this;
        }

        var entityType = typeof(TEntity);
        var param = Expression.Parameter(entityType, "x");
        var (propAccess, propertyType) = param.GetPropertyAccessAndType<TEntity>(filter.FieldName);

        if (propAccess == null || propertyType == null)
        {
            throw new BusinessLogicException("Некорректный фильтр: " + JsonSerializer.Serialize(filter));
        }

        var convertedValue = ConvertValue(filter.Value, propertyType, filter.OperationType);
        var constant = convertedValue != null ? Expression.Constant(convertedValue) : Expression.Constant(null);

        Expression condition;
        switch (filter.OperationType)
        {
            case FilterOperationType.Equals:
                condition = Expression.Equal(propAccess, constant);
                break;
            case FilterOperationType.NotEquals:
                condition = Expression.NotEqual(propAccess, constant);
                break;
            case FilterOperationType.GreaterThan:
                condition = Expression.GreaterThan(propAccess, constant);
                break;
            case FilterOperationType.GreaterThanOrEqual:
                condition = Expression.GreaterThanOrEqual(propAccess, constant);
                break;
            case FilterOperationType.LessThan:
                condition = Expression.LessThan(propAccess, constant);
                break;
            case FilterOperationType.LessThanOrEqual:
                condition = Expression.LessThanOrEqual(propAccess, constant);
                break;
            case FilterOperationType.Contains
                when propAccess.Type == typeof(string):
                var toLowerProp = Expression.Call(
                    propAccess,
                    typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!);

                if (constant is null)
                {
                    throw new ArgumentException($"Value can't be null for '{Enum.GetName(filter.OperationType)}' operation.");
                }

                if (constant.Value is not string stringValue)
                {
                    throw new ArgumentException($"'{Enum.GetName(filter.OperationType)}' operation allows only string values.");
                }

                var toLowerValue = Expression.Constant(stringValue.ToLower());
                var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;

                // x.SomeProp.ToLower().Contains(value.ToLower())
                var containsCall = Expression.Call(toLowerProp, containsMethod, toLowerValue);

                condition = containsCall;
                break;
            case FilterOperationType.StartsWith or FilterOperationType.EndsWith
                when propAccess.Type == typeof(string):
                var methodName = filter.OperationType switch
                {
                    FilterOperationType.Contains => nameof(string.Contains),
                    FilterOperationType.StartsWith => nameof(string.StartsWith),
                    _ => nameof(string.EndsWith)
                };
                var methodInfo = typeof(string).GetMethod(methodName, new[] { typeof(string) });
                condition = methodInfo is not null
                    ? Expression.Call(propAccess, methodInfo, constant)
                    : throw new ArgumentException($"{methodName} operation can only be used on string properties.");
                break;
            case FilterOperationType.In:
                condition = BuildInExpression(propAccess, constant, propertyType);
                break;
            case FilterOperationType.IsNull:
                condition = Expression.Equal(propAccess, Expression.Constant(null, propAccess.Type));
                break;
            case FilterOperationType.IsNotNull:
                condition = Expression.NotEqual(propAccess, Expression.Constant(null, propAccess.Type));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return AddFilter(Expression.Lambda<Func<TEntity, bool>>(condition, param));

        static Expression BuildInExpression(
            Expression prop,
            Expression valuesConstant,
            Type propertyType)
        {
            var listType = typeof(List<>).MakeGenericType(propertyType);
            if (!listType.IsAssignableFrom(valuesConstant.Type))
            {
                throw new InvalidCastException($"Cannot cast {valuesConstant.Type} to {listType}");
            }

            var method = typeof(Enumerable).GetMethods()
                .FirstOrDefault(m => m.Name == "Contains" && m.GetParameters().Length == 2)?
                .MakeGenericMethod(propertyType);

            return Expression.Call(method!, valuesConstant, prop);
        }
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
        var propToSort = sortOption.Key.ToLowerFirstChar();
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

    private static object? ConvertValue(object? value, Type propertyType, FilterOperationType operationType)
    {
        value = value is JsonElement jsonElement ? jsonElement.ToObject() : value;

        if (value is null)
            return null;

        if (operationType is not FilterOperationType.In)
        {
            return propertyType == typeof(string)
                ? value.ToString()
                : Convert.ChangeType(value, propertyType);
        }

        if (value is not IEnumerable collection)
        {
            throw new BusinessLogicException(
                $"Некорректный параметр фильтрации '{value}' " +
                $"для операции '{Enum.GetName(operationType)}'");
        }

        var listType = typeof(List<>).MakeGenericType(propertyType);
        var list = (IList)Activator.CreateInstance(listType)!;

        foreach (var item in collection)
            list.Add(Convert.ChangeType(item, propertyType));

        return list;
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
