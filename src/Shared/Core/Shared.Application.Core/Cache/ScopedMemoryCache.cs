// ----------------------------------------------------------------------------------------------
// <copyright file="ScopedMemoryCache.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shared.Domain.Core.Cache.Interfaces;

namespace Shared.Application.Core.Cache;

/// <summary>
/// Служба кэширования, ограниченная скоупом запроса.
/// </summary>
public class ScopedMemoryCache : IScopedMemoryCache
{
    private readonly ILogger<ScopedMemoryCache> _logger;
    private MemoryCache _cache;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ScopedMemoryCache"/>.
    /// </summary>
    /// <param name="logger">Логгер.</param>
    public ScopedMemoryCache(ILogger<ScopedMemoryCache> logger)
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _logger = logger;
        _logger.LogDebug("Создан новый экземпляр ScopedMemoryCache");
    }

    /// <inheritdoc />
    public T? GetOrCreate<T>(string key, Func<ICacheEntry, T> factory)
    {
        _logger.LogDebug("Запрос значения из кэша для ключа {Key}", key);
        return _cache.GetOrCreate(key, factory);
    }

    /// <inheritdoc />
    public Task<T?> GetOrCreateAsync<T>(string key, Func<ICacheEntry, Task<T>> factory)
    {
        _logger.LogDebug("Асинхронный запрос значения из кэша для ключа {Key}", key);
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
        _logger.LogDebug("Удаление значения из кэша для ключа {Key}", key);
        _cache.Remove(key);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _logger.LogDebug("Очистка всего кэша");
        _cache.Dispose();
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _logger.LogDebug("Освобождение ресурсов ScopedMemoryCache");
        _cache.Dispose();
    }
}
