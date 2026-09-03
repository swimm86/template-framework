// ----------------------------------------------------------------------------------------------
// <copyright file="MapperExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Domain.Core.Mapping.Extensions;

/// <summary>
/// Расширение для более удобной работы с <see cref="IMapper"/>.
/// </summary>
public static class MapperExtensions
{
    /// <summary>
    /// Преобразование (mapping) исходного типа <typeparamref name="TSource"/> в целевой тип <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TSource">Исходный тип для преобразования (mapping).</typeparam>
    /// <typeparam name="TResult">Целевой тип результата преобразования (mapping).</typeparam>
    /// <param name="source">Экземпляр исходного типа <typeparamref name="TSource"/>.</param>
    /// <param name="mapper">Сервис преобразования (mapping) объектов.</param>
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
    /// <param name="mapper">Сервис преобразования (mapping) объектов.</param>
    /// <param name="parameters">Необязательные параметры, используемые при проекции.</param>
    /// <returns>Коллекция проекций элементов в виде <see cref="IQueryable"/> целевого типа <typeparamref name="TResult"/>.</returns>
    public static IQueryable<TResult> ProjectTo<TResult>(
        this IQueryable source,
        IMapper mapper,
        object? parameters = null)
    {
        return mapper.ProjectTo<TResult>(source, parameters);
    }

    /// <summary>
    /// Преобразование (mapping) параметров из экземпляра исходного типа <typeparamref name="TSource"/> в экземпляр целевого типа <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TSource">Тип исходного экземпляра для преобразования (mapping).</typeparam>
    /// <typeparam name="TResult">Тип целевого экземпляра для преобразования (mapping).</typeparam>
    /// <param name="source">Экземпляр исходного типа <typeparamref name="TSource"/>.</param>
    /// <param name="result">Экземпляр целевого типа <typeparamref name="TResult"/>.</param>
    /// <param name="mapper">Сервис преобразования (mapping) объектов.</param>
    public static void Map<TSource, TResult>(this TSource source, TResult result, IMapper mapper)
    {
        mapper.Map(source, result);
    }
}