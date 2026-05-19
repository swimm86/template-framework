// ----------------------------------------------------------------------------------------------
// <copyright file="CacheServiceFactory.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Cache.Exceptions;
using Shared.Application.Core.Cache.Interfaces;

namespace Shared.Application.Core.Cache;

/// <summary>
/// Статический класс для регистрации и получения сервисов кэширования.
/// </summary>
public static class CacheServiceFactory
{
    /// <summary>
    /// Регистрирует сервис кэширования для указанного ключа.
    /// </summary>
    /// <typeparam name="TData">Тип кэшируемых данных.</typeparam>
    /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <param name="key">Ключ кэша.</param>
    /// <param name="getOrAddFunc">Функция получения или создания данных для кэша.</param>
    /// <returns>Коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection RegisterCacheService<TData>(
        this IServiceCollection serviceCollection,
        string key,
        Func<IServiceProvider, Task<TData>> getOrAddFunc)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(
                nameof(key),
                "Ключ кэша не может быть null или пустым.");
        }

        if (getOrAddFunc == null)
        {
            throw new ArgumentNullException(
                nameof(getOrAddFunc),
                "Функция, котрая возвращает даннные для кэширования не должна быть null.");
        }

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
    /// Возвращает экземпляр <see cref="ICacheService{TData}"/> по ключу.
    /// </summary>
    /// <typeparam name="TData">Тип кэшируемых данных.</typeparam>
    /// <param name="serviceProvider">Экземпляр <see cref="IServiceProvider"/>.</param>
    /// <param name="key">Ключ кэша.</param>
    /// <returns>Экземпляр <see cref="ICacheService{TData}"/>.</returns>
    /// <exception cref="CacheNotFoundException">Выбрасывается, если кэш с указанным ключом не найден.</exception>
    public static ICacheService<TData> GetCacheService<TData>(
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
    /// <param name="serviceProvider">Экземпляр <see cref="IServiceProvider"/>.</param>
    /// <param name="key">Ключ кэша.</param>
    /// <returns>Кэшированные данные типа <typeparamref name="TData"/>.</returns>
    /// <exception cref="CacheNotFoundException">Выбрасывается, если кэш с указанным ключом не найден.</exception>
    public static Task<TData> GetCachedDataAsync<TData>(
        this IServiceProvider serviceProvider,
        string key)
    {
        var cacheService = serviceProvider.GetKeyedService<ICacheService<TData>>(key);
        if (cacheService is null)
        {
            throw new CacheNotFoundException(key);
        }

        return cacheService.GetCachedDataAsync();
    }
}
