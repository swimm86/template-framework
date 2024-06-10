// ----------------------------------------------------------------------------------------------
// <copyright file="RepositorySpecification.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.Specification.Interfaces;
using System.Linq.Expressions;
using Shared.Domain.Core.Interfaces;
using Shared.Application.Core.Dal.Repository.Models;
using Shared.Common.Extensions;

namespace Shared.Application.Core.Dal.Specification.Models;

public abstract record RepositorySpecification<TEntity> : IRepositorySpecification<TEntity>
    where TEntity : class, IEntity
{
    private readonly QueryOptions<TEntity> _options = new();

    /// <inheritdoc />
    public abstract QueryOptions<TEntity> BuildOptions();

    /// <summary>
    /// Include если возвращается коллекция.
    /// </summary>
    /// <param name="expression">Include.</param>
    protected IIncludable<TProperty> AddInclude<TProperty>(Expression<Func<TEntity, ICollection<TProperty>>> expression)
    {
        var includable = new Includable<TProperty>(_options.Includes);
        includable.AddInclude(expression.GetPropertyName());

        return includable;
    }

    /// <summary>
    /// Include.
    /// </summary>
    /// <param name="include"></param>
    /// <typeparam name="TProperty"></typeparam>
    /// <returns></returns>
    protected IIncludable<TProperty> AddInclude<TProperty>(Expression<Func<TEntity, TProperty>> include)
    {
        var includable = new Includable<TProperty>(_options.Includes);
        includable.AddInclude(include.GetPropertyName());

        return includable;
    }

    /// <summary>
    /// Добавление фильтра.
    /// </summary>
    /// <param name="expression">Фильтр.</param>
    protected void AddFilter(Expression<Func<TEntity, bool>> expression)
    {
        _options.Filters.Add(expression);
    }

    /// <summary>
    /// Добавление сортировки.
    /// </summary>
    /// <param name="expression">Сортировка.</param>
    /// <param name="orderDirectionType">Направление сортировки.</param>
    protected void AddOrderBy(Expression<Func<TEntity, object>> expression, OrderDirectionType orderDirectionType)
    {
        _options.OrderBy.Add(new QueryOrderByOption<TEntity>(expression, orderDirectionType));
    }
}
