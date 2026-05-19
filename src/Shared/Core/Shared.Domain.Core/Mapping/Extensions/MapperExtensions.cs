// ----------------------------------------------------------------------------------------------
// <copyright file="MapperExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Domain.Core.Mapping.Extensions;

/// <summary>
/// Расширение для более удобной работы с <see cref="IMapper"/>.
/// </summary>
public static class MapperExtensions
{
    /// <summary>
    /// Маппинг исходного типа <see cref="TSource"/> в целевой тип <see cref="TResult"/>.
    /// </summary>
    /// <typeparam name="TSource">Исходный тип для маппинга.</typeparam>
    /// <typeparam name="TResult">Целевой тип результата маппинга.</typeparam>
    /// <param name="source">Экземпляр исходного типа <typeparamref name="TSource"/>.</param>
    /// <param name="mapper">Сервис маппинга объектов.</param>
    /// <returns>Экземпляр целевого типа <typeparamref name="TResult"/>.</returns>
    public static TResult Map<TSource, TResult>(this TSource source, IMapper mapper)
    {
        return mapper.Map<TSource, TResult>(source);
    }

    /// <summary>
    /// Проекция коллекции объектов типа <typeparamref name="TResult"/> из исходного <see cref="IQueryable"/>.
    /// </summary>
    /// <typeparam name="TResult">Целевой тип, в который будет выполнена проекция элементов.</typeparam>
    /// <param name="source">Коллекция в виде <see cref="IQueryable"/>, из которой будут проектироваться элементы.</param>
    /// <param name="mapper">Сервис маппинга объектов.</param>
    /// <param name="parameters">Необязательные параметры, используемые при проекции.</param>
    /// <param name="membersToExpand">Список выражений, определяющих свойства для раскрытия в проекции.</param>
    /// <returns>Коллекция проекций элементов в виде <see cref="IQueryable"/> целевого типа <typeparamref name="TResult"/>.</returns>
    public static IQueryable<TResult> ProjectTo<TResult>(
        this IQueryable source,
        IMapper mapper,
        object? parameters = null,
        params Expression<Func<TResult, object>>[] membersToExpand)
    {
        return mapper.ProjectTo(source, parameters, membersToExpand);
    }

    /// <summary>
    /// Маппинг параметров из экземпляра исходного типа <see cref="TSource"/> в экземпляр целевого типа <see cref="TResult"/>.
    /// </summary>
    /// <typeparam name="TSource">Тип исходного экземпляра для маппинга.</typeparam>
    /// <typeparam name="TResult">Тип целевого экземпляра для маппинга.</typeparam>
    /// <param name="source">Экземпляр исходного типа <typeparamref name="TSource"/>.</param>
    /// <param name="result">Экземпляр целевого типа <typeparamref name="TResult"/>.</param>
    /// <param name="mapper">Сервис маппинга объектов.</param>
    public static void Map<TSource, TResult>(this TSource source, TResult result, IMapper mapper)
    {
        mapper.Map(source, result);
    }
}