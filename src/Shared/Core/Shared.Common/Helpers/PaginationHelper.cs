// ----------------------------------------------------------------------------------------------
// <copyright file="PaginationHelper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Common.Helpers;

/// <summary>
/// Класс утилит для пагинации.
/// </summary>
public static class PaginationHelper
{
    /// <summary>
    /// Получение кол-ва всех страниц
    /// </summary>
    /// <param name="totalCount">Общее кол-во элементов.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <returns>Кол-во страниц.</returns>
    public static int GetTotalPages(int? totalCount, int? pageSize)
    {
        if (!pageSize.HasValue || !totalCount.HasValue)
        {
            return 0;
        }

        return (int)Math.Ceiling((double)totalCount.Value / pageSize.Value);
    }

    /// <summary>
    /// Посчитать параметры пагинации
    /// </summary>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <returns>Параметры пагинации.</returns>
    public static (int? skip, int? take) CalculatePagination(int? pageNumber, int? pageSize)
    {
        if (!pageNumber.HasValue || !pageSize.HasValue)
        {
            return (null, null);
        }

        const int minPageAmount = 1;
        var skip = (pageNumber - minPageAmount) * pageSize;
        var take = pageSize;

        return (skip, take);
    }
}
