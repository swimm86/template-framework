// ----------------------------------------------------------------------------------------------
// <copyright file="TypeExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections;
using System.Reflection;

namespace Shared.Common.Extensions;

/// <summary>
/// Методы расширения для <see cref="Type"/>.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Провереяет, реализует ли тип <see cref="IEnumerable"/> или <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <param name="type">Тип для проверки.</param>
    /// <returns>Результат проверки.</returns>
    public static bool ImplementsIEnumerable(this Type type)
    {
        if (type == null)
            return false;

        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            return true;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return true;

        // Рекурсивно проверяем все интерфейсы
        foreach (var interfaceType in type.GetInterfaces())
        {
            if (interfaceType.ImplementsIEnumerable())
                return true;
        }

        // Для массивов
        if (type.IsArray && type.GetElementType()!.ImplementsIEnumerable())
            return true;

        // Для nullable-типов
        if (Nullable.GetUnderlyingType(type) is Type underlyingType && underlyingType.ImplementsIEnumerable())
            return true;

        return false;
    }

    /// <summary>
    /// Получает <see cref="PropertyInfo"/> типа по наименованию свойства, игнорируя регистр.
    /// </summary>
    /// <param name="type">Тип.</param>
    /// <param name="propertyName">Наименование свойства.</param>
    /// <returns><see cref="PropertyInfo"/> или <see langword="null"/>, если свойство не было найдено.</returns>
    public static PropertyInfo? GetPropertyIgnoreCase(this Type type, string propertyName) =>
        type.GetProperty(
            propertyName.Trim(),
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
}
