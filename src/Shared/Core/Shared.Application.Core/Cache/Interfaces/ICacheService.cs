// ----------------------------------------------------------------------------------------------
// <copyright file="ICacheService.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Cache.Interfaces;

/// <summary>
/// Интерфейс сервиса кэширования.
/// </summary>
/// <typeparam name="TData">Тип кэшируемых данных.</typeparam>
public interface ICacheService<TData>
{
    /// <summary>
    /// Обновляет данные в кэше.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task UpdateCacheAsync();

    /// <summary>
    /// Возвращает данные из кэша.
    /// </summary>
    /// <returns>Кэшированные данные типа <typeparamref name="TData"/>.</returns>
    Task<TData> GetCachedDataAsync();
}