// ----------------------------------------------------------------------------------------------
// <copyright file="IncludableExtension.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Repository.Models;

namespace Shared.Domain.Core.Dal.Repository.Extensions;

/// <summary>
/// Расширения для <see cref="IIncludable{TProperty}"/>.
/// </summary>
public static class IncludableExtension
{
    /// <summary>
    /// ThenInclude, если на предыдущем шаге была выбрана коллекция.
    /// </summary>
    /// <typeparam name="TSrcEntity">Начальный тип сущности.</typeparam>
    /// <typeparam name="TPreviosProp">Тип промежуточного свойства.</typeparam>
    /// <typeparam name="TProp">Тип целевого свойства.</typeparam>
    /// <param name="includable">Расширяемый объект.</param>
    /// <param name="expression">Выражение.</param>
    /// <returns><see cref="Includable{TSrcEntity,TProp}"/>.</returns>
    public static Includable<TSrcEntity, TProp> ThenInclude<TSrcEntity, TPreviosProp, TProp>(
        this Includable<TSrcEntity, ICollection<TPreviosProp>> includable,
        Expression<Func<TPreviosProp, TProp>> expression)
    {
        var thenIncludable = new Includable<TSrcEntity, TProp>(expression);
        includable.SetChild(thenIncludable);
        return thenIncludable;
    }

    /// <summary>
    /// ThenInclude, если на предыдущем шаге была выбрана фильтрованная коллекция.
    /// </summary>
    /// <typeparam name="TSrcEntity">Начальный тип сущности.</typeparam>
    /// <typeparam name="TPreviosProp">Тип промежуточного свойства.</typeparam>
    /// <typeparam name="TProp">Тип целевого свойства.</typeparam>
    /// <param name="includable">Расширяемый объект.</param>
    /// <param name="expression">Выражение.</param>
    /// <returns><see cref="Includable{TSrcEntity,TProp}"/>.</returns>
    public static Includable<TSrcEntity, TProp> ThenInclude<TSrcEntity, TPreviosProp, TProp>(
        this Includable<TSrcEntity, IEnumerable<TPreviosProp>> includable,
        Expression<Func<TPreviosProp, TProp>> expression)
    {
        var thenIncludable = new Includable<TSrcEntity, TProp>(expression);
        includable.SetChild(thenIncludable);
        return thenIncludable;
    }

    /// <summary>
    /// ThenInclude, если на предыдущем шаге было выбрано плоское свойство.
    /// </summary>
    /// <typeparam name="TSrcEntity">Начальный тип сущности.</typeparam>
    /// <typeparam name="TPreviosProp">Тип промежуточного свойства.</typeparam>
    /// <typeparam name="TProp">Тип целевого свойства.</typeparam>
    /// <param name="includable">Расширяемый объект.</param>
    /// <param name="expression">Выражение.</param>
    /// <returns><see cref="Includable{TSrcEntity,TProp}"/>.</returns>
    public static Includable<TSrcEntity, TProp> ThenInclude<TSrcEntity, TPreviosProp, TProp>(
        this Includable<TSrcEntity, TPreviosProp> includable,
        Expression<Func<TPreviosProp, TProp>> expression)
    {
        var thenIncludable = new Includable<TSrcEntity, TProp>(expression);
        includable.SetChild(thenIncludable);
        return thenIncludable;
    }
}
