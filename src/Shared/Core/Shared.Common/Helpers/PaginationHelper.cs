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
    /// Получение количества всех страниц.
    /// </summary>
    /// <remarks>
    /// Возвращает <c>0</c>, если <paramref name="totalCount"/> или <paramref name="pageSize"/> равны <c>null</c>,
    /// либо если <paramref name="pageSize"/> равен <c>0</c> (защита от деления на ноль).
    /// </remarks>
    /// <param name="totalCount">Общее количество элементов (должно быть неотрицательным).</param>
    /// <param name="pageSize">Размер страницы (должен быть неотрицательным).</param>
    /// <returns>Количество страниц.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Если <paramref name="totalCount"/> или <paramref name="pageSize"/> отрицательны.
    /// </exception>
    public static int GetTotalPages(int? totalCount, int? pageSize)
    {
        if (!pageSize.HasValue || !totalCount.HasValue)
        {
            return 0;
        }

        ArgumentOutOfRangeException.ThrowIfNegative(totalCount.Value, nameof(totalCount));
        ArgumentOutOfRangeException.ThrowIfNegative(pageSize.Value, nameof(pageSize));

        if (pageSize.Value == 0)
        {
            return 0;
        }

        return (int)Math.Ceiling((double)totalCount.Value / pageSize.Value);
    }

    /// <summary>
    /// Рассчитывает параметры пагинации (<c>skip</c>/<c>take</c>) для постраничной выборки.
    /// </summary>
    /// <remarks>
    /// Возвращает <c>(null, null)</c>, если <paramref name="pageNumber"/> или <paramref name="pageSize"/> равны <c>null</c>
    /// (потребитель должен интерпретировать это как «пагинация не запрошена»).
    /// Нумерация страниц — с <c>1</c>.
    /// </remarks>
    /// <param name="pageNumber">Номер страницы (1-based, должен быть &gt;= 1).</param>
    /// <param name="pageSize">Размер страницы (должен быть &gt;= 1).</param>
    /// <returns>Кортеж <c>(skip, take)</c> либо <c>(null, null)</c> при null-входах.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Если <paramref name="pageNumber"/> или <paramref name="pageSize"/> меньше <c>1</c>.
    /// </exception>
    public static (int? skip, int? take) CalculatePagination(int? pageNumber, int? pageSize)
    {
        if (!pageNumber.HasValue || !pageSize.HasValue)
        {
            return (null, null);
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber.Value, 1, nameof(pageNumber));
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize.Value, 1, nameof(pageSize));

        var skip = (pageNumber.Value - 1) * pageSize.Value;
        return (skip, pageSize);
    }
}
