// ----------------------------------------------------------------------------------------------
// <copyright file="IncludableExtension.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Common.Extensions;
using Shared.Domain.Core.Dal.Specification.Interfaces;
using Shared.Domain.Core.Dal.Specification.Models;

namespace Shared.Domain.Core.Dal.Specification.Extensions;

/// <summary>
/// Расшения для <see cref="IIncludable{TProperty}"/>.
/// </summary>
public static class IncludableExtension
{
    /// <summary>
    /// ThenInclude если возвращается единственная сущность.
    /// </summary>
    /// <param name="includable">Расширяемый объект.</param>
    /// <param name="navigationProperty">Выражение.</param>
    /// <typeparam name="TPreviousProperty">Предыдущий тип.</typeparam>
    /// <typeparam name="TProperty">Возвращаемый тип.</typeparam>
    /// <returns><see cref="IIncludable{TProperty}"/>.</returns>
    public static IIncludable<TProperty> ThenInclude<TPreviousProperty, TProperty>(
        this IIncludable<TPreviousProperty> includable,
        Expression<Func<TPreviousProperty, TProperty>> navigationProperty)
    {
        var next = new Includable<TProperty>(includable.Includes);
        var listSize = next.Includes.Count;
        var lastElem = next.Includes[listSize - 1];
        next.Includes[listSize - 1] = string.Join('.', lastElem, navigationProperty.GetPropertyName());

        return next;
    }

    /// <summary>
    /// ThenInclude если возвращается коллекция сущностей.
    /// </summary>
    /// <param name="includable">Расширяемый объект.</param>
    /// <param name="navigationProperty">Выражение.</param>
    /// <typeparam name="TPreviousProperty">Предыдущий тип.</typeparam>
    /// <typeparam name="TProperty">Возвращаемый тип.</typeparam>
    /// <returns><see cref="IIncludable{TProperty}"/>.</returns>
    public static IIncludable<TProperty> ThenInclude<TPreviousProperty, TProperty>(
        this IIncludable<ICollection<TPreviousProperty>> includable,
        Expression<Func<TPreviousProperty, TProperty>> navigationProperty)
    {
        var next = new Includable<TProperty>(includable.Includes);
        var listSize = next.Includes.Count;
        var lastElem = next.Includes[listSize - 1];
        next.Includes[listSize - 1] = string.Join('.', lastElem, navigationProperty.GetPropertyName());

        return next;
    }

    /// <summary>
    /// ThenInclude если возвращается коллекция сущностей.
    /// </summary>
    /// <param name="includable">Расширяемый объект.</param>
    /// <param name="navigationProperty">Выражение.</param>
    /// <typeparam name="TPreviousProperty">Предыдущий тип.</typeparam>
    /// <typeparam name="TProperty">Возвращаемый тип.</typeparam>
    /// <returns><see cref="IIncludable{TProperty}"/>.</returns>
    public static IIncludable<TProperty> ThenInclude<TPreviousProperty, TProperty>(
        this IIncludable<IReadOnlyCollection<TPreviousProperty>> includable,
        Expression<Func<TPreviousProperty, TProperty>> navigationProperty)
    {
        var next = new Includable<TProperty>(includable.Includes);
        var listSize = next.Includes.Count;
        var lastElem = next.Includes[listSize - 1];
        next.Includes[listSize - 1] = string.Join('.', lastElem, navigationProperty.GetPropertyName());

        return next;
    }
}
