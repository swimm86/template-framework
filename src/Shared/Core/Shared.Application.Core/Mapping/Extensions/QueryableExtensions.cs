// ----------------------------------------------------------------------------------------------
// <copyright file="QueryableExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Application.Core.Mapping.Interfaces;

namespace Shared.Application.Core.Mapping.Extensions;

/// <summary>
/// Расширение для <see cref="IQueryable{T}"/>.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Проецирует исходный запрос <paramref name="source"/> в <see cref="IQueryable{T}"/>
    /// элементов типа <typeparamref name="TDestination"/> используя заданный <paramref name="mapper"/>.
    /// </summary>
    /// <typeparam name="TDestination">Целевой тип элементов результата запроса.</typeparam>
    /// <param name="source">Исходный запрос для проекции.</param>
    /// <param name="mapper">Маппер, который определяет правила преобразования типов.</param>
    /// <param name="parameters">Необязательные параметры, используемые маппером при проекции.</param>
    /// <param name="membersToExpand">Выражения для указания членов, которые нужно развернуть.</param>
    /// <returns>Новый экземпляр <see cref="IQueryable{T}"/>, содержащий элементы типа <typeparamref name="TDestination"/>.</returns>
    public static IQueryable<TDestination> ProjectTo<TDestination>(
        this IQueryable source,
        IMapper mapper,
        object? parameters = null,
        params Expression<Func<TDestination, object>>[] membersToExpand) =>
        mapper.ProjectTo(source, parameters, membersToExpand);
}
