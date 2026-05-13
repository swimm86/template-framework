// ----------------------------------------------------------------------------------------------
// <copyright file="Mapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using AutoMapper.QueryableExtensions;
using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Infrastructure.Mapper.AutoMapper;

/// <summary>
/// Маппер.
/// </summary>
public class Mapper(
    global::AutoMapper.IMapper mapper)
    : IMapper
{
    /// <inheritdoc />
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        return mapper.Map<TSource, TDestination>(source);
    }

    /// <inheritdoc />
    public IQueryable<TDestination> ProjectTo<TDestination>(
        IQueryable source,
        object? parameters = null,
        params Expression<Func<TDestination, object>>[] membersToExpand)
    {
        var sourceType = source.GetType();
        return sourceType is { IsGenericType: true, GenericTypeArguments.Length: 1 } &&
               typeof(TDestination) == sourceType.GenericTypeArguments[0]
            ? (source as IQueryable<TDestination>)!
            : source.ProjectTo(mapper.ConfigurationProvider, parameters, membersToExpand);
    }

    /// <inheritdoc />
    public void Map<TSource, TResult>(TSource source, TResult result)
    {
        mapper.Map(source, result);
    }
}
