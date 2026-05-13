// ----------------------------------------------------------------------------------------------
// <copyright file="IObjectToStringConverter.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Converters.Interfaces;

/// <summary>
/// Интерфейс для конвертеров объекта в строку.
/// </summary>
public interface IObjectToStringConverter
{
    /// <summary>
    /// Конвертирует объект в строку.
    /// </summary>
    /// <param name="valueToConvert">Объект, который необходимо сконвертировать.</param>
    /// <returns>Результат конвертации.</returns>
    string Convert(object? valueToConvert);
}