// ----------------------------------------------------------------------------------------------
// <copyright file="IMapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Mapping.Interfaces;

/// <summary>
/// Интерфейс преобразователя (mapper).
/// </summary>
public interface IMapper
{
    /// <summary>
    /// Преобразование (mapping) исходного типа <typeparamref name="TSource"/> в целевой тип <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TSource">Исходный тип для преобразования (mapping).</typeparam>
    /// <typeparam name="TResult">Целевой тип результата преобразования (mapping).</typeparam>
    /// <param name="source">Экземпляр исходного типа <typeparamref name="TSource"/>.</param>
    /// <returns>Экземпляр целевого типа <typeparamref name="TResult"/>.</returns>
    TResult Map<TSource, TResult>(TSource source);

    /// <summary>
    /// Проекция коллекции объектов типа <typeparamref name="TResult"/> из исходного <see cref="IQueryable"/>.
    /// </summary>
    /// <typeparam name="TResult">Целевой тип, в который будет выполнена проекция элементов.</typeparam>
    /// <param name="source">Коллекция в виде <see cref="IQueryable"/>, из которой будут проектироваться элементы.</param>
    /// <param name="parameters">Необязательные параметры, используемые при проекции.</param>
    /// <returns>Коллекция проекций элементов в виде <see cref="IQueryable"/> целевого типа <typeparamref name="TResult"/>.</returns>
    IQueryable<TResult> ProjectTo<TResult>(
        IQueryable source,
        object? parameters = null);

    /// <summary>
    /// Преобразование (mapping) параметров из экземпляра исходного типа <typeparamref name="TSource"/> в экземпляр целевого типа <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TSource">Тип исходного экземпляра для преобразования (mapping).</typeparam>
    /// <typeparam name="TResult">Тип целевого экземпляра для преобразования (mapping).</typeparam>
    /// <param name="source">Экземпляр исходного типа <typeparamref name="TSource"/>.</param>
    /// <param name="result">Экземпляр целевого типа <typeparamref name="TResult"/>.</param>
    void Map<TSource, TResult>(TSource source, TResult result);
}
