// ----------------------------------------------------------------------------------------------
// <copyright file="ConvertUsingFuncWrapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Infrastructure.Mapper.AutoMapper.Adapters.Wrappers;

/// <summary>
/// Обёртка функции-конвертера (type converter) для AutoMapper <c>ConvertUsing</c>
/// (без <c>ctx</c>). Получает существующий целевой объект, но игнорирует его
/// (Func-конвертер без dest его не использует).
/// </summary>
/// <typeparam name="TSource">Исходный тип.</typeparam>
/// <typeparam name="TDestination">Целевой тип.</typeparam>
internal sealed class ConvertUsingFuncWrapper<TSource, TDestination>(
    Func<object, object> func)
{
    /// <summary>
    /// Преобразует исходный объект через функцию-конвертер без параметра <c>destination</c>
    /// и возвращает результат конвертации.
    /// </summary>
    /// <param name="source">Исходный объект.</param>
    /// <param name="dest">Существующий целевой объект; в этой реализации не используется, поскольку Func-конвертер без параметра <c>destination</c>.</param>
    /// <returns>Результат конвертации.</returns>
    public TDestination Invoke(TSource source, TDestination dest)
    {
        return (TDestination)func(source!);
    }
}
