// ----------------------------------------------------------------------------------------------
// <copyright file="PageableRequest.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Batch;
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
    /// Номер страницы (нумерация с единицы).
    /// </summary>
    /// <remarks>
    /// Значение по умолчанию для свойства — <c>1</c>: при отсутствии поля в теле JSON десериализатор использует это значение вместо неявного <c>0</c> для типа <see cref="int"/>.
    /// Это согласовано с постраничными методами расширения, которые отклоняют <c>PageNumber &lt; 1</c>.
    /// </remarks>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Размер страницы.
    /// </summary>
    /// <remarks>По умолчанию: 100 (<see cref="Constants.DefaultBatchSize"/>).</remarks>
    public int PageSize { get; set; } = Constants.DefaultBatchSize;

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
                    key: string.Join(ValueDelimiter, sortOptionValues[..^1]),
                    directionType: GetDirectionType(
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
