// ----------------------------------------------------------------------------------------------
// <copyright file="IScopedMemoryCache.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Caching.Memory;

namespace Shared.Domain.Core.Cache.Interfaces;

/// <summary>
/// Интерфейс для службы кэширования, ограниченной скоупом запроса.
/// </summary>
public interface IScopedMemoryCache : IDisposable
{
    /// <summary>
    /// Получает значение из кэша по указанному ключу, либо создает и добавляет его, если ключ отсутствует.
    /// </summary>
    /// <typeparam name="T">Тип кэшируемого значения.</typeparam>
    /// <param name="key">Ключ для доступа к значению.</param>
    /// <param name="factory">Фабрика для создания значения, если оно отсутствует в кэше.</param>
    /// <returns>Значение из кэша или созданное фабрикой.</returns>
    T? GetOrCreate<T>(string key, Func<ICacheEntry, T> factory);

    /// <summary>
    /// Получает значение из кэша по указанному ключу, либо создает и добавляет его, если ключ отсутствует.
    /// </summary>
    /// <typeparam name="T">Тип кэшируемого значения.</typeparam>
    /// <param name="key">Ключ для доступа к значению.</param>
    /// <param name="factory">Асинхронная фабрика для создания значения, если оно отсутствует в кэше.</param>
    /// <returns>Значение из кэша или созданное фабрикой.</returns>
    Task<T?> GetOrCreateAsync<T>(string key, Func<ICacheEntry, Task<T>> factory);

    /// <summary>
    /// Проверяет наличие значения в кэше.
    /// </summary>
    /// <typeparam name="T">Тип кэшируемого значения.</typeparam>
    /// <param name="key">Ключ для проверки.</param>
    /// <param name="value">Значение, присутствующее в кэше.</param>
    /// <returns>True, если значение присутствует в кэше; иначе, false.</returns>
    bool TryGetValue<T>(string key, out T? value);

    /// <summary>
    /// Удаляет значение из кэша по указанному ключу.
    /// </summary>
    /// <param name="key">Ключ для удаления.</param>
    void Remove(string key);

    /// <summary>
    /// Очищает весь кэш.
    /// </summary>
    void Clear();
}
