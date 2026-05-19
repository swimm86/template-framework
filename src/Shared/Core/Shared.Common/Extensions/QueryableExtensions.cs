// ----------------------------------------------------------------------------------------------
// <copyright file="QueryableExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Common.Extensions;

/// <summary>
/// Методы расширения для <see cref="IQueryable{T}"/>: постраничная выборка, ограничение результата.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Возвращает подмножество элементов из <see cref="IQueryable{T}"/> с указанием пропуска и количества.
    /// </summary>
    /// <typeparam name="TEntity">Тип элементов в <see cref="IQueryable{T}"/>.</typeparam>
    /// <param name="query">Исходный запрос <see cref="IQueryable{T}"/>.</param>
    /// <param name="skip">Количество элементов для пропуска. Если не указан — пропуск не применяется.</param>
    /// <param name="take">Количество элементов для выборки. Если не указан — возвращаются все элементы.</param>
    /// <returns>Запрос <see cref="IQueryable{T}"/> с применёнными ограничениями Skip/Take.</returns>
    public static IQueryable<TEntity> GetRange<TEntity>(
        this IQueryable<TEntity> query,
        int? skip = null,
        int? take = null)
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
