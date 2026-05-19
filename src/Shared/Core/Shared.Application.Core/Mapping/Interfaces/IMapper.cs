// ----------------------------------------------------------------------------------------------
// <copyright file="IMapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;

namespace Shared.Application.Core.Mapping.Interfaces;

/// <summary>
/// Интерфейс сервиса маппинга объектов.
/// </summary>
public interface IMapper
{
    /// <summary>
    /// Выполняет маппинг экземпляра исходного типа в целевой тип (<see cref="TSource"/>-><see cref="TResult"/>).
    /// </summary>
    /// <typeparam name="TSource">Исходный тип.</typeparam>
    /// <typeparam name="TResult">Целевой тип.</typeparam>
    /// <param name="source">Экземпляр исходного типа.</param>
    /// <returns>Экземпляр целевого типа.</returns>
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
    /// Выполняет маппинг свойств из исходного экземпляра в целевой (<see cref="TSource"/>-><see cref="TResult"/>).
    /// </summary>
    /// <typeparam name="TSource">Исходный тип.</typeparam>
    /// <typeparam name="TResult">Целевой тип.</typeparam>
    /// <param name="source">Экземпляр исходного типа.</param>
    /// <param name="result">Экземпляр целевого типа.</param>
    void Map<TSource, TResult>(TSource source, TResult result);
}
