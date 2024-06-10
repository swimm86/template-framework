// ----------------------------------------------------------------------------------------------
// <copyright file="Mapper.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using AutoMapper.QueryableExtensions;
using Shared.Application.Core.Mapping.Interfaces;

namespace Shared.Infrastructure.Mapper.AutoMapper;

/// <summary>
/// Маппер.
/// </summary>
public class Mapper(
    global::AutoMapper.IMapper mapper
) : IMapper
{
    /// <summary>
    /// Маппинг к конкретному типу.
    /// </summary>
    /// <typeparam name="TSource">Входной тип.</typeparam>
    /// <typeparam name="TDestination">Выходной тип.</typeparam>
    /// <param name="source">Входной объект.</param>
    /// <returns>Результат маппинга.</returns>
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        return mapper.Map<TSource, TDestination>(source);
    }

    /// <summary>
    /// Проектирует IQueryable исходных данных в коллекцию объектов целевого типа.
    /// </summary>
    /// <typeparam name="TDestination">Выходной тип.</typeparam>
    /// <param name="source">Входной IQueryable объект.</param>
    /// <param name="parameters">Дополнительные параметры.</param>
    /// <param name="membersToExpand">Выражения для раскрытия членов объектов.</param>
    /// <returns>IQueryable коллекция объектов типа TDestination.</returns>
    public IQueryable<TDestination> ProjectTo<TDestination>(
        IQueryable source, 
        object? parameters = null,
        params Expression<Func<TDestination, object>>[] membersToExpand)
    {
        return source.ProjectTo(mapper.ConfigurationProvider, parameters, membersToExpand);
    }
}