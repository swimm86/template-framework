// ----------------------------------------------------------------------------------------------
// <copyright file="ReadListQuery.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Requests;

namespace Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;

/// <summary>
/// Базовый запрос на чтение коллекции сущностей с пагинацией.
/// </summary>
/// <typeparam name="TRequest">Тип запроса с параметрами пагинации.</typeparam>
/// <typeparam name="TFilter">Тип фильтра для отбора данных.</typeparam>
/// <typeparam name="TResponse">Тип возвращаемого значения.</typeparam>
/// <param name="request">Запрос с параметрами пагинации и фильтрации.</param>
public abstract class ReadListQuery<TRequest, TFilter, TResponse>(
    TRequest request)
    : IQuery<TResponse>
    where TRequest : PageableRequest<TFilter>
    where TFilter : new()
{
    /// <summary>
    /// Минимальный допустимый номер страницы.
    /// </summary>
    private const int MinPageNumber = 1;

    /// <summary>Номер страницы для пагинации.</summary>
    public int PageNumber { get; } = request.PageNumber < MinPageNumber ? MinPageNumber : request.PageNumber;

    /// <summary>Количество элементов на странице.</summary>
    public int? PageSize { get; } = request.PageSize;

    /// <summary>Исходный запрос с параметрами.</summary>
    public TRequest Request => request;

    /// <summary>Фильтр для отбора данных.</summary>
    public TFilter Filter { get; } = request.Filter ?? new TFilter();
}
