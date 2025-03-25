// ----------------------------------------------------------------------------------------------
// <copyright file="EfQueryEvaluator.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Dal;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Repository;

/// <summary>
/// Реализация интерфейса <see cref="IQueryEvaluator"/> на основе ORM "Entity Framework Core"
/// </summary>
public class EfQueryEvaluator(IMapper mapper) : IQueryEvaluator
{
    /// <inheritdoc />>
    public IQueryable<TEntity> Build<TEntity>(
        IQueryable<TEntity> queryable,
        QueryOptions<TEntity>? options = null)
        where TEntity : class, IEntity
    {
        if (options is null)
        {
            return queryable;
        }

        // Применяем фильтры
        queryable = options.Filters
            .Aggregate(queryable, (acc, x) => acc.Where(x));

        // Применяем Includes-ы
        queryable = options.Includes
            .Aggregate(queryable, (acc, x) => acc.Include(x));

        // Применяем порядок сортировки
        if (options.OrderBy.Count != 0)
        {
            var firstOrderBy = options.OrderBy.First();
            var queryableOrderedBy = firstOrderBy.Direction == OrderDirectionType.Ascending
                ? queryable.OrderBy(firstOrderBy.Expression)
                : queryable.OrderByDescending(firstOrderBy.Expression);

            queryable = options.OrderBy.Count == 1
                ? queryableOrderedBy
                : options.OrderBy.Skip(1)
                    .Aggregate(queryableOrderedBy, (acc, x) => x.Direction == OrderDirectionType.Ascending
                        ? acc.ThenBy(x.Expression)
                        : acc.ThenByDescending(x.Expression));
        }

        if (!options.WithTracking)
        {
            queryable = queryable.AsNoTracking();
        }

        if (options.AsSplitQuery)
        {
            queryable = queryable.AsSplitQuery();
        }

        if (options.Distinct)
        {
            queryable = queryable.Distinct();
        }

        if (options.DistinctBy is not null)
        {
            queryable = queryable.DistinctBy(options.DistinctBy);
        }

        return queryable;
    }

    /// <inheritdoc />>
    public IQueryable<TOut> BuildWithTransform<TEntity, TOut>(
        IQueryable<TEntity> queryable,
        QueryOptions<TEntity>? options = null)
        where TEntity : class, IEntity
    {
        var result = Build(queryable, options);
        if (result is IQueryable<TOut> tOutQuery)
        {
            return tOutQuery;
        }

        return mapper.ProjectTo<TOut>(result);
    }
}
