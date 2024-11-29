// ----------------------------------------------------------------------------------------------
// <copyright file="IMapper.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;

namespace Shared.Domain.Core.Mapping.Interfaces;

/// <summary>
/// Интерфейс маппера.
/// </summary>
public interface IMapper
{
    /// <summary>
    /// Маппинг исходного типа <see cref="TSource"/> в целевой тип <see cref="TResult"/>.
    /// </summary>
    /// <typeparam name="TSource">Исходный тип для маппинга.</typeparam>
    /// <typeparam name="TResult">Целевой тип результата маппинга.</typeparam>
    /// <param name="source">Экземпляр исходного типа <typeparamref name="TSource"/>.</param>
    /// <returns>Экземпляр целевого типа <typeparamref name="TResult"/>.</returns>
    TResult Map<TSource, TResult>(TSource source);

    /// <summary>
    /// Проекция коллекции объектов типа <typeparamref name="TResult"/> из исходного <see cref="IQueryable"/>.
    /// </summary>
    /// <typeparam name="TResult">Целевой тип, в который будет выполнена проекция элементов.</typeparam>
    /// <param name="source">Коллекция в виде <see cref="IQueryable"/>, из которой будут проектироваться элементы.</param>
    /// <param name="parameters">Необязательные параметры, используемые при проекции.</param>
    /// <param name="membersToExpand">Список выражений, определяющих свойства для раскрытия в проекции.</param>
    /// <returns>Коллекция проекций элементов в виде <see cref="IQueryable"/> целевого типа <typeparamref name="TResult"/>.</returns>
    IQueryable<TResult> ProjectTo<TResult>(
        IQueryable source,
        object? parameters = null,
        params Expression<Func<TResult, object>>[] membersToExpand);

    /// <summary>
    /// Маппинг параметров из экземпляра исходного типа <see cref="TSource"/> в экземпляр целевого типа <see cref="TResult"/>.
    /// </summary>
    /// <typeparam name="TSource">Тип исходного экземпляра для маппинга.</typeparam>
    /// <typeparam name="TResult">Тип целевого экземпляра для маппинга.</typeparam>
    /// <param name="source">Экземпляр исходного типа <typeparamref name="TSource"/>.</param>
    /// <param name="result">Экземпляр целевого типа <typeparamref name="TSource"/>.</param>
    void Map<TSource, TResult>(TSource source, TResult result);
}
