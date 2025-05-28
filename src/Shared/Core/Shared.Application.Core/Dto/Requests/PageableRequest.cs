// ----------------------------------------------------------------------------------------------
// <copyright file="PageableRequest.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Helpers;
using Shared.Domain.Core.Dal;
using Shared.Domain.Core.Dal.Models;

namespace Shared.Application.Core.Dto.Requests;

/// <summary>
/// Базовая модель для Request-а с пагинацией.
/// </summary>
public abstract record PageableRequest
{
    /// <summary>
    /// Разделитель значений в объекте строки.
    /// </summary>
    public const char ValueDelimiter = '.';

    /// <summary>
    /// Номер страницы.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Размер страницы.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Настройки сортировки.
    /// </summary>
    public List<string>? SortOptions { get; init; }

    /// <summary>
    /// Преобразует настройки сортировки в коллекцию экземпляров класса <see cref="SortOption"/>.
    /// </summary>
    /// <returns>Коллекция экземпляров класса <see cref="SortOption"/>.</returns>
    public ICollection<SortOption> ConvertSortOptions()
    {
        if (!SortOptions?.Any() ?? true)
        {
            return [];
        }

        return SortOptions
            .Where(value => !string.IsNullOrEmpty(value))
            .Select(value =>
            {
                var sortOptionValues = value.Split(ValueDelimiter);
                return new SortOption(
                    Key: string.Join(ValueDelimiter, sortOptionValues[..^1]),
                    DirectionType: GetDirectionType(
                        sortOptionValues.ElementAtOrDefault(sortOptionValues.Length - 1)));
            })
            .ToList();
    }

    private static OrderDirectionType GetDirectionType(string? str) =>
        EnumHelper.GetEnumByDescription(str?.ToLower(), OrderDirectionType.Ascending);
}

/// <summary>
/// Базовая модель для Request-а с пагинацией.
/// </summary>
public abstract record PageableRequest<TFilter>
    : PageableRequest
    where TFilter : new()
{
    /// <summary>
    /// Фильтр.
    /// </summary>
    public TFilter? Filter { get; init; } = new();
}
