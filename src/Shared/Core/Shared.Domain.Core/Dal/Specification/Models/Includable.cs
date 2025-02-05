// ----------------------------------------------------------------------------------------------
// <copyright file="Includable.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.Specification.Interfaces;

namespace Shared.Domain.Core.Dal.Specification.Models;

/// <summary>
/// Кастомная навигации
/// </summary>
/// <typeparam name="TEntity">Начальная сущность</typeparam>
/// <typeparam name="TCurrent">Текущая сущность</typeparam>
/// <typeparam name="TNext">Свойство</typeparam>
public class Includable<TEntity, TCurrent, TNext>(List<IncludeNode> includes)
    : IIncludable<TEntity, TCurrent, TNext>
{
    /// <inheritdoc/>
    public List<IncludeNode> Includes { get; private set; } = includes;

    /// <inheritdoc/>
    public IIncludable<TEntity, TEntity, TProperty> AddInclude<TProperty>(
        Expression<Func<TEntity, ICollection<TProperty>>> expression)
    {
        Includes.Add(new IncludeNode(expression, typeof(TEntity), typeof(ICollection<TProperty>)));
        return new Includable<TEntity, TEntity, TProperty>(Includes);
    }

    /// <inheritdoc/>
    public IIncludable<TEntity, TEntity, TProperty> AddInclude<TProperty>(
        Expression<Func<TEntity, IEnumerable<TProperty>>> expression)
    {
        Includes.Add(new IncludeNode(expression, typeof(TEntity), typeof(IEnumerable<TProperty>)));
        return new Includable<TEntity, TEntity, TProperty>(Includes);
    }

    /// <inheritdoc/>
    public IIncludable<TEntity, TEntity, TProperty> AddInclude<TProperty>(
        Expression<Func<TEntity, List<TProperty>>> expression)
    {
        Includes.Add(new IncludeNode(expression, typeof(TEntity), typeof(List<TProperty>)));
        return new Includable<TEntity, TEntity, TProperty>(Includes);
    }

    /// <inheritdoc/>
    public IIncludable<TEntity, TEntity, TProperty> AddInclude<TProperty>(
        Expression<Func<TEntity, TProperty>> expression)
    {
        Includes.Add(new IncludeNode(expression, typeof(TEntity), typeof(TProperty)));
        return new Includable<TEntity, TEntity, TProperty>(Includes);
    }

    /// <inheritdoc/>
    public IIncludable<TEntity, TNext, TProperty> ThenInclude<TProperty>(
        Expression<Func<TNext, IEnumerable<TProperty>>> expression)
    {
        Includes.Add(new IncludeNode(expression, typeof(TNext), typeof(IEnumerable<TProperty>), typeof(TEntity)));
        return new Includable<TEntity, TNext, TProperty>(Includes);
    }

    /// <inheritdoc/>
    public IIncludable<TEntity, TNext, TProperty> ThenInclude<TProperty>(
        Expression<Func<TNext, ICollection<TProperty>>> expression)
    {
        Includes.Add(new IncludeNode(expression, typeof(TNext), typeof(ICollection<TProperty>), typeof(TEntity)));
        return new Includable<TEntity, TNext, TProperty>(Includes);
    }

    /// <inheritdoc/>
    public IIncludable<TEntity, TNext, TProperty> ThenInclude<TProperty>(
        Expression<Func<TNext, List<TProperty>>> expression)
    {
        Includes.Add(new IncludeNode(expression, typeof(TNext), typeof(List<TProperty>), typeof(TEntity)));
        return new Includable<TEntity, TNext, TProperty>(Includes);
    }

    /// <inheritdoc/>
    public IIncludable<TEntity, TNext, TProperty> ThenInclude<TProperty>(
        Expression<Func<TNext, TProperty>> expression)
    {
        Includes.Add(new IncludeNode(expression, typeof(TNext), typeof(TProperty), typeof(TEntity)));
        return new Includable<TEntity, TNext, TProperty>(Includes);
    }
}
