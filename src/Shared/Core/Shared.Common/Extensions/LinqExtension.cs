// ----------------------------------------------------------------------------------------------
// <copyright file="LinqExtension.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Common.Extensions;

/// <summary>
/// Методы расширения для <see cref="IEnumerable{T}"/> и <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
public static class LinqExtension
{
    /// <summary>
    /// Выполняет указанное действие для каждого элемента последовательности.
    /// </summary>
    /// <typeparam name="T">Тип элементов последовательности.</typeparam>
    /// <param name="source">Исходная последовательность.</param>
    /// <param name="action">Действие, выполняемое для каждого элемента.</param>
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
    /// Асинхронно выполняет указанное действие для каждого элемента последовательности.
    /// </summary>
    /// <typeparam name="T">Тип элементов последовательности.</typeparam>
    /// <param name="source">Исходная последовательность.</param>
    /// <param name="func">Асинхронное действие, выполняемое для каждого элемента.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
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
    /// Асинхронно выполняет указанное действие для каждого элемента асинхронной последовательности.
    /// </summary>
    /// <typeparam name="T">Тип элементов последовательности.</typeparam>
    /// <param name="source">Исходная асинхронная последовательность.</param>
    /// <param name="func">Асинхронное действие, выполняемое для каждого элемента.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
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
