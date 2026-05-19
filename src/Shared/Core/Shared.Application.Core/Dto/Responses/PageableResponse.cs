// ----------------------------------------------------------------------------------------------
// <copyright file="PageableResponse.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;

namespace Shared.Application.Core.Dto.Responses;

/// <summary>
/// Данные с пагинацией.
/// </summary>
/// <typeparam name="T">Тип данных.</typeparam>
public record PageableResponse<T> : Response<T>
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="PageableResponse{T}"/>.
    /// </summary>
    public PageableResponse()
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="PageableResponse{T}"/>.
    /// </summary>
    /// <param name="totalPages">Всего страниц.</param>
    /// <param name="pageNumber">Номер текущей страницы.</param>
    /// <param name="payload">Полезная нагрузка.</param>
    /// <param name="statusCode">Статус ответа.</param>
    public PageableResponse(int totalPages, int pageNumber, T? payload, int statusCode = StatusCodes.Status200OK)
        : base(payload, statusCode)
    {
        TotalPages = totalPages;
        PageNumber = pageNumber;
    }

    /// <summary>
    /// Всего страниц.
    /// </summary>
    public int TotalPages { get; init; }

    /// <summary>
    /// Номер текущей страницы.
    /// </summary>
    public int PageNumber { get; init; }
}
