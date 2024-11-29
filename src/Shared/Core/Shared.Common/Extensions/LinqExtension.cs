// ----------------------------------------------------------------------------------------------
// <copyright file="LinqExtension.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Common.Extensions;

/// <summary>
/// Расширение для <see cref="IEnumerable{T}"/>.
/// </summary>
public static class LinqExtension
{
    /// <summary>
    /// <see cref="ForEach{T}"/> для <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">Тип перечисления.</typeparam>
    /// <param name="source">Перечисление.</param>
    /// <param name="action">Операция.</param>
    public static void ForEach<T>(
        this IEnumerable<T> source,
        Action<T> action)
    {
        foreach (var element in source)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            action(element);
        }
    }

    /// <summary>
    /// Асинхронная версия <see cref="ForEach{T}"/> для <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">ип перечисления.</typeparam>
    /// <param name="source">Перечисление.</param>
    /// <param name="func">Операция.</param>
    /// <returns><see cref="Task"/>.</returns>
    public static async Task ForeachAsync<T>(
        this IEnumerable<T> source,
        Func<T, Task> func)
    {
        foreach (var item in source)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            await func(item);
        }
    }
}
