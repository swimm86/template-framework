// ----------------------------------------------------------------------------------------------
// <copyright file="ScopedMemoryCache.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shared.Domain.Core.Cache.Interfaces;

namespace Shared.Application.Core.Cache;

/// <summary>
/// Сервис кэширования, ограниченный областью видимости запроса (scoped).
/// </summary>
/// <param name="logger">Экземпляр <see cref="ILogger{ScopedMemoryCache}"/> для работы с логированием.</param>
public class ScopedMemoryCache(
    ILogger<ScopedMemoryCache> logger)
    : IScopedMemoryCache
{
    private MemoryCache _cache = new(new MemoryCacheOptions());

    /// <inheritdoc />
    public T? GetOrCreate<T>(string key, Func<ICacheEntry, T> factory)
    {
        logger.LogDebug("Cache value request for key {Key}", key);
        return _cache.GetOrCreate(key, factory);
    }

    /// <inheritdoc />
    public Task<T?> GetOrCreateAsync<T>(string key, Func<ICacheEntry, Task<T>> factory)
    {
        logger.LogDebug("Async cache value request for key {Key}", key);
        return _cache.GetOrCreateAsync(key, factory);
    }

    /// <inheritdoc />
    public bool TryGetValue<T>(string key, out T? value)
    {
        return _cache.TryGetValue(key, out value);
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        logger.LogDebug("Removing cache value for key {Key}", key);
        _cache.Remove(key);
    }

    /// <inheritdoc />
    public void Clear()
    {
        logger.LogDebug("Clearing the entire cache");
        _cache.Dispose();
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        logger.LogDebug("Disposing {scopedMemoryCache} resources", nameof(ScopedMemoryCache));
        _cache.Dispose();
    }
}
