// ----------------------------------------------------------------------------------------------
// <copyright file="QueryableExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using System.Reflection;
using Shared.Common.Extensions;
using Shared.Domain.Core.Dal.Repository.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Repository.Extensions;

/// <summary>
/// Расширения для <see cref="IQueryable{T}"/>.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Метод Inlude из <see cref="EntityFrameworkQueryableExtensions"/>.
    /// </summary>
    private static readonly MethodInfo IncludeMethod = typeof(EntityFrameworkQueryableExtensions)
        .GetMethods()
        .First(m => m.Name == nameof(EntityFrameworkQueryableExtensions.Include) &&
            m.GetParameters().Length == 2 &&
            m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>));

    /// <summary>
    /// Методы ThenInlude из <see cref="EntityFrameworkQueryableExtensions"/>.
    /// </summary>
    private static readonly List<MethodInfo> ThenIncludeMethods = typeof(EntityFrameworkQueryableExtensions)
        .GetMethods()
        .Where(m => m.Name == nameof(EntityFrameworkQueryableExtensions.ThenInclude) &&
                m.GetParameters().Length == 2 &&
                m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>))
        .ToList();

    /// <summary>
    /// Собирает Include-ы для <see cref="IQueryable{T}"/>.
    /// </summary>
    /// <typeparam name="TEntity">Тип начальной сущности.</typeparam>
    /// <param name="queryable">Расширяемый объект.</param>
    /// <param name="includable"><see cref="IIncludable{TSrcEntity}"/>.</param>
    /// <returns>IQueryable с вызванными Include и ThenInclude.</returns>
    public static IQueryable<TEntity> IncludeUntyped<TEntity>(
        this IQueryable<TEntity> queryable,
        IIncludable<TEntity> includable)
    {
        var entityType = typeof(TEntity);
        var propertyType = includable.Expression.ReturnType;

        var genericInclude = IncludeMethod.MakeGenericMethod(entityType, propertyType);

        var result = genericInclude.Invoke(
            null,
            [queryable, includable.Expression])!;

        var child = includable.Child;

        while (child is not null)
        {
            // выбираем подходящий метод ThenInclude на основании, реализует ли тип IEnumerable<T>
            var thenIncludeMethod = propertyType.ImplementsIEnumerable()
                ? ThenIncludeMethods[0]
                : ThenIncludeMethods[1];

            var returnType = child.Expression.ReturnType;

            var method = thenIncludeMethod.MakeGenericMethod(
                entityType,
                propertyType.IsGenericType ? propertyType.GenericTypeArguments[0] : propertyType,
                returnType);

            result = method.Invoke(
                null,
                [result, child.Expression])!;

            propertyType = child.Expression.ReturnType;
            child = child.Child;
        }

        return (IQueryable<TEntity>)result;
    }
}
