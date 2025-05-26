// ----------------------------------------------------------------------------------------------
// <copyright file="TypeExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections;

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
}
