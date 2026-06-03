// ----------------------------------------------------------------------------------------------
// <copyright file="CacheService.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Cache.Interfaces;

namespace Shared.Application.Core.Cache;

/// <summary>
/// Сервис работы с кэшем, реализующий <see cref="ICacheService{TData}"/>.
/// Поддерживает асинхронное обновление и получение кэшированных данных.
/// </summary>
/// <typeparam name="TData">Тип кэшируемых данных.</typeparam>
/// <param name="key">Ключ кэша.</param>
/// <param name="serviceProvider">Провайдер сервисов для получения зависимостей.</param>
/// <param name="getFunc">Функция получения данных при отсутствии в кэше.</param>
public class CacheService<TData>(
    string key,
    IServiceProvider serviceProvider,
    Func<IServiceProvider, Task<TData>> getFunc)
    : ICacheService<TData>
{
    private readonly IMemoryCache _cache = serviceProvider.GetRequiredService<IMemoryCache>();

    private readonly object _sync = new();
    private Task<TData>? _cacheCreationTask;

    /// <inheritdoc />
    public Task UpdateCacheAsync()
    {
        lock (_sync)
        {
            // Если создание кэша уже в процессе, возвращаем текущую задачу.
            if (_cacheCreationTask is { IsCompleted: false })
            {
                return _cacheCreationTask;
            }

            _cache.Remove(key);
            return _cacheCreationTask = GetCachedDataAsync();
        }
    }

    /// <inheritdoc />
    public Task<TData> GetCachedDataAsync() =>
        _cache.GetOrCreateAsync(key, _ => getFunc(serviceProvider))!;
}
