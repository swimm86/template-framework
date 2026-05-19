// ----------------------------------------------------------------------------------------------
// <copyright file="DefaultObjectPoolExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.ObjectPool;

namespace Shared.Application.Core.Extensions;

/// <summary>
/// Методы расширения для <see cref="DefaultObjectPool{T}"/>.
/// </summary>
public static class DefaultObjectPoolExtensions
{
    /// <summary>
    /// Получает объект из пула, выполняет действие и возвращает объект обратно в пул.
    /// </summary>
    /// <typeparam name="TPoolObject">Тип объекта в пуле.</typeparam>
    /// <param name="pool">Пул объектов.</param>
    /// <param name="action">Действие для выполнения с объектом из пула.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="pool"/> или <paramref name="action"/> равен <see langword="null"/>.
    /// </exception>
    public static void UsePool<TPoolObject>(
        this DefaultObjectPool<TPoolObject> pool,
        Action<TPoolObject> action)
        where TPoolObject : class
    {
        var poolObject = pool.Get();
        try
        {
            action(poolObject);
        }
        finally
        {
            pool.Return(poolObject);
        }
    }

    /// <summary>
    /// Получает объект из пула, выполняет функцию и возвращает объект обратно в пул.
    /// </summary>
    /// <typeparam name="TPoolObject">Тип объекта в пуле.</typeparam>
    /// <typeparam name="TReturnValue">Тип возвращаемого значения.</typeparam>
    /// <param name="pool">Пул объектов.</param>
    /// <param name="func">Функция для выполнения с объектом из пула.</param>
    /// <returns>Результат выполнения функции.</returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="pool"/> или <paramref name="func"/> равен <see langword="null"/>.
    /// </exception>
    public static TReturnValue UsePool<TPoolObject, TReturnValue>(
        this DefaultObjectPool<TPoolObject> pool,
        Func<TPoolObject, TReturnValue> func)
        where TPoolObject : class
    {
        var poolObject = pool.Get();
        try
        {
            return func(poolObject);
        }
        finally
        {
            pool.Return(poolObject);
        }
    }

    /// <summary>
    /// Асинхронно получает объект из пула, выполняет асинхронное действие и возвращает объект обратно в пул.
    /// </summary>
    /// <typeparam name="TPoolObject">Тип объекта в пуле.</typeparam>
    /// <param name="pool">Пул объектов.</param>
    /// <param name="actionTask">Асинхронное действие для выполнения с объектом из пула.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="pool"/> или <paramref name="actionTask"/> равен <see langword="null"/>.
    /// </exception>
    public static async Task UsePoolAsync<TPoolObject>(
        this DefaultObjectPool<TPoolObject> pool,
        Func<TPoolObject, Task> actionTask)
        where TPoolObject : class
    {
        var poolObject = pool.Get();
        try
        {
            await actionTask(poolObject);
        }
        finally
        {
            pool.Return(poolObject);
        }
    }

    /// <summary>
    /// Асинхронно получает объект из пула, выполняет асинхронную функцию и возвращает объект обратно в пул.
    /// </summary>
    /// <typeparam name="TPoolObject">Тип объекта в пуле.</typeparam>
    /// <typeparam name="TReturnValue">Тип возвращаемого значения.</typeparam>
    /// <param name="pool">Пул объектов.</param>
    /// <param name="func">Асинхронная функция для выполнения с объектом из пула.</param>
    /// <returns>Задача, представляющая асинхронную операцию с результатом.</returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="pool"/> или <paramref name="func"/> равен <see langword="null"/>.
    /// </exception>
    public static async Task<TReturnValue> UsePoolAsync<TPoolObject, TReturnValue>(
        this DefaultObjectPool<TPoolObject> pool,
        Func<TPoolObject, Task<TReturnValue>> func)
        where TPoolObject : class
    {
        var poolObject = pool.Get();
        try
        {
            return await func(poolObject);
        }
        finally
        {
            pool.Return(poolObject);
        }
    }
}
