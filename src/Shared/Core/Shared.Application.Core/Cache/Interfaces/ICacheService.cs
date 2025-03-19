// ----------------------------------------------------------------------------------------------
// <copyright file="ICacheService.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Cache.Interfaces;

/// <summary>
/// Интерфейс для работы с кэшем.
/// </summary>
/// <typeparam name="TData">Тип кэшируемых данных.</typeparam>
public interface ICacheService<TData>
{
    /// <summary>
    /// Обновляет кэш асинхронно.
    /// </summary>
    /// <returns>Асинхронная задача.</returns>
    Task UpdateCacheAsync();

    /// <summary>
    /// Возвращает данные из кэша асинхронно.
    /// </summary>
    /// <returns>Асинхронная задача, возвращающая данные типа <see cref="TData"/>.</returns>
    Task<TData> GetCachedDataAsync();
}