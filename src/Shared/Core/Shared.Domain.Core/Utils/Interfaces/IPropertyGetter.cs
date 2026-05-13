// ----------------------------------------------------------------------------------------------
// <copyright file="IPropertyGetter.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Converters;
using Shared.Domain.Core.Converters.Interfaces;

namespace Shared.Domain.Core.Utils.Interfaces;

/// <summary>
/// Интерфейс для извлечения значений свойств из объектов.
/// </summary>
public interface IPropertyGetter
{
    /// <summary>
    /// Получает значение свойства объекта.
    /// </summary>
    /// <param name="obj">Объект, из которого необходимо получить значение свойства.</param>
    /// <param name="propertyName">Название свойства, значение которого необходимо получить.</param>
    /// <param name="throwIfNotFound">
    /// Если <c>true</c> (по умолчанию) — выбрасывает <see cref="InvalidOperationException"/>, когда свойство не найдено.
    /// Если <c>false</c> — возвращает <c>null</c>.
    /// </param>
    /// <returns>Значение свойства, либо <c>null</c> если свойство не найдено и <paramref name="throwIfNotFound"/> равен <c>false</c>.</returns>
    object? GetProperty(
        object? obj,
        string propertyName,
        bool throwIfNotFound = true);

    /// <summary>
    /// Получает строковое представление значения свойства объекта.
    /// </summary>
    /// <param name="obj">Объект, из которого необходимо получить значение свойства.</param>
    /// <param name="propertyName">Название свойства, значение которого необходимо получить.</param>
    /// <param name="converter">Конвертер объекта в строку. По умолчанию используется <see cref="DefaultObjectToStringConverter"/>.</param>
    /// <returns>Строковое представление значения свойства.</returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если свойство с именем <paramref name="propertyName"/> не найдено.</exception>
    string GetPropertyAsString(
        object obj,
        string propertyName,
        IObjectToStringConverter? converter = null);
}