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
    ///  <see cref="ForEach{T}"/> для <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">Тип перечисления.</typeparam>
    /// <param name="source">Перечисление.</param>
    /// <param name="action">Операция.</param>
    public static void ForEach<T>(
        this IEnumerable<T> source,
        Action<T> action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        foreach (var element in source)
        {
            action(element);
        }
    }

    /// <summary>
    /// Асинхронная версия <see cref="ForEach{T}"/> для <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">ип перечисления.</typeparam>
    /// <param name="source">Перечисление.</param>
    /// <param name="func">Операция.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns><see cref="Task"/>.</returns>
    public static async Task ForeachAsync<T>(
        this IEnumerable<T> source,
        Func<T, Task> func,
        CancellationToken cancellationToken = default)
    {
        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await func(item);
        }
    }


    /// <summary>
    /// Асинхронная версия <see cref="ForEach{T}"/> для <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">ип перечисления.</typeparam>
    /// <param name="source">Перечисление.</param>
    /// <param name="func">Операция.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns><see cref="Task"/>.</returns>
    public static async Task ForEachAsync<T>(
        this IAsyncEnumerable<T> source,
        Func<T, Task> func,
        CancellationToken cancellationToken = default)
    {
        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await func(item);
        }
    }
}
