// ----------------------------------------------------------------------------------------------
// <copyright file="EfQueryEvaluator.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using System.Reflection;
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
            .Aggregate(queryable, (acc, filter) => acc.Where(filter));

        object? currentQuery = queryable;

        var includeChains = new List<List<IncludeNode>>();
        List<IncludeNode>? currentChain = null;
        foreach (var include in options.Includes)
        {
            if (include.PreviousType == null)
            {
                if (currentChain != null && currentChain.Any())
                {
                    includeChains.Add(currentChain);
                }

                currentChain = new List<IncludeNode> { include };
            }
            else
            {
                if (currentChain == null)
                {
                    currentChain = new List<IncludeNode> { include };
                }
                else
                {
                    currentChain.Add(include);
                }
            }
        }

        if (currentChain != null && currentChain.Any())
        {
            includeChains.Add(currentChain);
        }

        var query = queryable;
        foreach (var chain in includeChains)
        {
            var root = chain[0];
            query = CallInclude(query, root.Expression, root.DestinationType);

            object currentIncludable = query;

            for (int i = 1; i < chain.Count; i++)
            {
                var node = chain[i];
                var previousNode = chain[i - 1];
                bool isCollection = typeof(System.Collections.IEnumerable).IsAssignableFrom(previousNode.DestinationType);

                if (isCollection)
                {
                    Type elementType = GetElementType(previousNode.DestinationType);
                    currentIncludable = CallThenIncludeForCollection<TEntity>(
                        currentIncludable, node.Expression, elementType, node.DestinationType);
                }
                else
                {
                    currentIncludable = CallThenIncludeForReference<TEntity>(
                        currentIncludable, node.Expression, previousNode.DestinationType, node.DestinationType);
                }
            }

            query = currentIncludable as IQueryable<TEntity> ?? query;
        }

        // Применяем сортировку
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

        return query;
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

    private static IQueryable<TEntity> CallInclude<TEntity>(
        IQueryable<TEntity> source,
        LambdaExpression navigationPropertyPath,
        Type destinationType)
    {
        var includeMethod = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == "Include" && m.GetParameters().Length == 2);
        var genericMethod = includeMethod.MakeGenericMethod(typeof(TEntity), destinationType);
        var result = genericMethod.Invoke(null, new object[] { source, navigationPropertyPath });

        return (IQueryable<TEntity>)result!;
    }

    private static object CallThenIncludeForReference<TEntity>(
        object source,
        LambdaExpression navigationPropertyPath,
        Type previousPropertyType,
        Type destinationType)
    {
        var thenIncludeMethod = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "ThenInclude" && m.GetParameters().Length == 2)
            .First(m =>
            {
                var paramType = m.GetParameters()[0].ParameterType;
                var genericArg = paramType.GetGenericArguments()[1];
                return !(genericArg.IsGenericType && genericArg.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            });
        var genericMethod = thenIncludeMethod.MakeGenericMethod(typeof(TEntity), previousPropertyType, destinationType);

        return genericMethod.Invoke(null, new object[] { source, navigationPropertyPath })!;
    }

    private static object CallThenIncludeForCollection<TEntity>(
        object source,
        LambdaExpression navigationPropertyPath,
        Type elementType,
        Type destinationType)
    {
        var thenIncludeMethod = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "ThenInclude" && m.GetParameters().Length == 2)
            .First(m =>
            {
                var paramType = m.GetParameters()[0].ParameterType;
                var genericArg = paramType.GetGenericArguments()[1];

                return genericArg.IsGenericType && genericArg.GetGenericTypeDefinition() == typeof(IEnumerable<>);
            });
        var genericMethod = thenIncludeMethod.MakeGenericMethod(typeof(TEntity), elementType, destinationType);

        return genericMethod.Invoke(null, new object[] { source, navigationPropertyPath })!;
    }

    private static Type GetElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType()!;
        if (type.IsGenericType && type.GetGenericArguments().Length == 1)
            return type.GetGenericArguments()[0];
        var iface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return iface?.GetGenericArguments()[0] ?? type;
    }
}
