// ----------------------------------------------------------------------------------------------
// <copyright file="LinqExtension.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Common.Extensions;

/// <summary>
/// Расширение для <see cref="IEnumerable{T}"/>.
/// </summary>
public static class LinqExtension
{
    /// <summary>
    /// ForEach для <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">Тип перечисления.</typeparam>
    /// <param name="enumerable">Перечисление.</param>
    /// <param name="action">Операция.</param>
    public static void ForEach<T>(
        this IEnumerable<T> enumerable,
        Action<T> action)
    {
        foreach (var element in enumerable)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            action(element);
        }
    }
}
