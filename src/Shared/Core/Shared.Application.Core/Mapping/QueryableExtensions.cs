// ----------------------------------------------------------------------------------------------
// <copyright file="QueryableExtensions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Application.Core.Mapping.Interfaces;

namespace Shared.Application.Core.Mapping;

/// <summary>
/// Расширения для <see cref="IQueryable"/>.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Осуществляет проекцию данных в <see cref="TDestination"/>.
    /// </summary>
    /// <typeparam name="TDestination">Тип назначения.</typeparam>
    /// <param name="source">Исходные данные.</param>
    /// <param name="mapper">Маппер.</param>
    /// <param name="parameters">Параметры маппера.</param>
    /// <param name="membersToExpand">Поля для расширения.</param>
    /// <returns></returns>
    public static IQueryable<TDestination> ProjectTo<TDestination>(
        this IQueryable source,
        IMapper mapper,
        object? parameters = null,
        params Expression<Func<TDestination, object>>[] membersToExpand
    ) => mapper.ProjectTo(source, parameters, membersToExpand);
}