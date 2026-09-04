// ----------------------------------------------------------------------------------------------
// <copyright file="PageableRequest.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Batch;
using Shared.Common.Extensions;
using Shared.Domain.Core.Dal;
using Shared.Domain.Core.Dal.Models;

namespace Shared.Application.Core.Dto.Requests;

/// <summary>
/// Базовая модель запроса с пагинацией.
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
    /// <remarks>
    /// Формат элемента: <c>"key.direction"</c>, где разделитель — <see cref="ValueDelimiter"/>.
    /// Если направление не указано (строка не содержит разделителя), сортировка выполняется
    /// по возрастанию (<see cref="OrderDirectionType.Ascending"/>).
    /// Префиксные и постфиксные пробелы вокруг ключа и направления игнорируются.
    /// </remarks>
    /// <returns>Коллекция экземпляров класса <see cref="SortOption"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Выбрасывается при невалидном входе: пустой ключ (например, <c>".asc"</c> или <c>"."</c>),
    /// пустой сегмент направления (например, <c>"Name."</c>),
    /// или если сегмент направления не соответствует ни одному элементу
    /// <see cref="OrderDirectionType"/> по его <c>Description</c>-атрибуту (например, <c>"Name.invalid"</c>).
    /// </exception>
    public ICollection<SortOption> ConvertSortOptions()
    {
        if (SortOptions?.Any() != true)
        {
            return [];
        }

        return SortOptions
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value =>
            {
                var segments = value
                    .Split(ValueDelimiter)
                    .Select(segment => segment.Trim())
                    .ToArray();

                if (segments.Length == 1)
                {
                    return new SortOption(
                        key: segments[0],
                        directionType: OrderDirectionType.Ascending);
                }

                var key = string.Join(ValueDelimiter, segments[..^1]);
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new ArgumentException(
                        $"Invalid sort key in '{value}': key must not be empty.");
                }

                return new SortOption(
                    key: key,
                    directionType: GetDirectionType(segments[^1]));
            })
            .ToList();
    }

    private static OrderDirectionType GetDirectionType(string? str) =>
        str.GetEnumValueByDescription<OrderDirectionType>()
            ?? throw new ArgumentException($"Invalid sort direction: '{str}'");
}

/// <summary>
/// Базовая модель запроса с пагинацией и фильтром.
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
