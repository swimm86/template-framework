// ----------------------------------------------------------------------------------------------
// <copyright file="QueryableExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Common.Extensions;

/// <summary>
/// Расширение для <see cref="IQueryable{T}"/>.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Возвращает подмножество элементов из <see cref="IQueryable{T}"/> с возможностью указания пропуска и количества элементов для получения.
    /// </summary>
    /// <typeparam name="TEntity">Тип элементов в <see cref="IQueryable{T}"/>.</typeparam>
    /// <param name="query">Исходный <see cref="IQueryable{T}"/>.</param>
    /// <param name="skip">Количество элементов, которые нужно пропустить. Если не указан, то не происходит пропуска.</param>
    /// <param name="take">Количество элементов, которые нужно получить. Если не указан, то возвращаются все элементы.</param>
    /// <returns>Новый <see cref="IQueryable{T}"/>, содержащий подмножество элементов из исходного <see cref="IQueryable{T}"/>.</returns>
    public static IQueryable<TEntity> GetRange<TEntity>(
        this IQueryable<TEntity> query,
        int? skip = default,
        int? take = default)
    {
        if (skip != null)
        {
            query = query.Skip(skip.Value);
        }

        if (take != null)
        {
            query = query.Take(take.Value);
        }

        return query;
    }
}
