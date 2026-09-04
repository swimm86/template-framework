// ----------------------------------------------------------------------------------------------
// <copyright file="ITypeConverter.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Mapping.Interfaces;

/// <summary>
/// Конвертер типов, провайдеро-независимый аналог AutoMapper ITypeConverter.
/// </summary>
/// <typeparam name="TSource">Исходный тип.</typeparam>
/// <typeparam name="TDestination">Целевой тип.</typeparam>
public interface ITypeConverter<in TSource, TDestination>
{
    /// <summary>
    /// Преобразует исходный объект в целевой.
    /// </summary>
    /// <param name="source">Исходный объект.</param>
    /// <param name="destination">Целевой объект (может быть пустым при первичном создании).</param>
    /// <param name="context">Провайдеро-независимый контекст маппинга (mapping context).</param>
    /// <returns>Преобразованный целевой объект.</returns>
    TDestination Convert(TSource source, TDestination destination, ResolutionContext context);
}
