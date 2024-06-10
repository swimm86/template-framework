// ----------------------------------------------------------------------------------------------
// <copyright file="EfQueryEvaluator.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.Dal.Repository.Models;
using Shared.Application.Core.Mapping.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Repository;

/// <summary>
/// Реализация интерфейса <see cref="IQueryEvaluator"/> на основе ORM "Entity Framework Core"
/// </summary>
public class EfQueryEvaluator(IMapper mapper) : IQueryEvaluator
{
    /// <inheritdoc />>
    public IQueryable<TEntity> Build<TEntity>(
        IQueryable<TEntity> queryable,
        QueryOptions<TEntity> options
    ) where TEntity : class, IEntity
    {
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

        if (!options.WithTracking) queryable = queryable.AsNoTracking();

        return queryable;
    }

    /// <inheritdoc />>
    public IQueryable<TOut> BuildWithTransform<TEntity, TOut>(
        IQueryable<TEntity> queryable,
        QueryOptions<TEntity> options
    ) where TEntity : class, IEntity
    {
        return mapper.ProjectTo<TOut>(Build(queryable, options));
    }
}
