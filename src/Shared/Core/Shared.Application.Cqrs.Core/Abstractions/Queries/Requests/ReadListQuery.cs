// ----------------------------------------------------------------------------------------------
// <copyright file="ReadListQuery.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Requests;
using Shared.Application.Cqrs.Core.Utils;
using Shared.Common.Helpers;
using Shared.Domain.Core.Dal;

namespace Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;

/// <summary>
/// Бзаовый класс для чтения с пагинацией
/// </summary>
/// <typeparam name="TRequest">Запрос.</typeparam>
/// /// <typeparam name="TFilter">Фильтр.</typeparam>
/// <typeparam name="TResponse">Ожидаемый ответ.</typeparam>
public abstract class ReadListQuery<TRequest, TFilter, TResponse>(TRequest request)
    : IQuery<TResponse>
    where TRequest : PageableRequest<TFilter>
    where TFilter : new()
{
    /// <summary>
    /// Разделитель значений в объекте строки.
    /// </summary>
    public const char ValueDelimiter = '.';

    /// <summary>
    /// Минимальный номер страницы.
    /// </summary>
    private const int MinPageNumber = 1;

    /// <summary>
    /// Номер страницы.
    /// </summary>
    public int PageNumber { get; } = request.PageNumber < MinPageNumber ? MinPageNumber : request.PageNumber;

    /// <summary>
    /// Количество элементов на одной странице.
    /// </summary>
    public int? PageSize { get; } = request.PageSize;

    /// <summary>
    /// Запрос.
    /// </summary>
    public TRequest Request => request;

    /// <summary>
    /// Фильтр.
    /// </summary>
    public TFilter Filter { get; } = request.Filter ?? new TFilter();

    /// <summary>
    /// Параметры сортировки.
    /// </summary>
    public List<SortOption> SortOptions { get; } = ConvertSortOptions(request.SortOptions);

    private static List<SortOption> ConvertSortOptions(List<string>? sortOptions)
    {
        if (sortOptions is null)
        {
            return [];
        }

        return sortOptions
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
