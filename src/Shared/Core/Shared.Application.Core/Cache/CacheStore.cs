// ----------------------------------------------------------------------------------------------
// <copyright file="CacheStore.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Cache.Exceptions;
using Shared.Application.Core.Cache.Interfaces;

namespace Shared.Application.Core.Cache;

/// <summary>
/// Статический класс для управления кэшем.
/// Позволяет регистрировать и получать кэшированные данные.
/// </summary>
public static class CacheStore
{
    /// <summary>
    /// Регистрирует новый кэш для указанного ключа.
    /// </summary>
    /// <typeparam name="TData">Тип кэшируемых данных.</typeparam>
    /// <param name="serviceCollection">Экземпляр <see cref="IServiceCollection"/> для работы с ним.</param>
    /// <param name="key">Ключ для кэширования данных.</param>
    /// <param name="getOrAddFunc">Функция для получения или добавления данных в кэш.</param>
    /// <returns>Экземпляр <see cref="IServiceCollection"/> для работы с ним.</returns>
    public static IServiceCollection RegisterCache<TData>(
        this IServiceCollection serviceCollection,
        string key,
        Func<IServiceProvider, Task<TData>> getOrAddFunc)
    {
        return serviceCollection
            .AddMemoryCache()
            .AddKeyedSingleton<ICacheService<TData>, CacheService<TData>>(
                key,
                (serviceProvider, _) => new CacheService<TData>(
                    key,
                    serviceProvider,
                    getOrAddFunc));
    }

    /// <summary>
    /// Возвращает <see cref="ICacheService{TData}"/>.
    /// </summary>
    /// <typeparam name="TData">Тип кэшируемых данных.</typeparam>
    /// <param name="serviceProvider">Экземпляр <see cref="IServiceProvider"/> для работы с ним.</param>
    /// <param name="key">Ключ для получения данных из кэша.</param>
    /// <returns>Асинхронная задача, возвращающая кэшированные данные.</returns>
    /// <returns>Экземпляр <see cref="ICacheService{TData}"/>.</returns>
    /// <exception cref="CacheNotFoundException">Выбрасывается, если кэш с указанным ключом не найден.</exception>
    public static ICacheService<TData> GetCacheServiceAsync<TData>(
        this IServiceProvider serviceProvider,
        string key)
    {
        var cacheService =
            serviceProvider.GetKeyedService<ICacheService<TData>>(key);
        if (cacheService is null)
        {
            throw new CacheNotFoundException(key);
        }

        return cacheService;
    }

    /// <summary>
    /// Получает данные из кэша по указанному ключу.
    /// </summary>
    /// <typeparam name="TData">Тип кэшируемых данных.</typeparam>
    /// <param name="serviceProvider">Экземпляр <see cref="IServiceProvider"/> для работы с ним.</param>
    /// <param name="key">Ключ для получения данных из кэша.</param>
    /// <returns>Асинхронная задача, возвращающая кэшированные данные.</returns>
    /// <exception cref="CacheNotFoundException">Выбрасывается, если кэш с указанным ключом не найден.</exception>
    public static Task<TData> GetCacheAsync<TData>(
        this IServiceProvider serviceProvider,
        string key)
    {
        var cacheService =
            serviceProvider?.GetKeyedService<ICacheService<TData>>(key);
        if (cacheService is null)
        {
            throw new CacheNotFoundException(key);
        }

        return cacheService.GetCachedDataAsync();
    }
}
