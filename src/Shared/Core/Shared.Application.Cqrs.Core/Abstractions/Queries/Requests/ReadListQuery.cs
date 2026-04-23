// ----------------------------------------------------------------------------------------------
// <copyright file="ReadListQuery.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Requests;

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
}
