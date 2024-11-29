// ----------------------------------------------------------------------------------------------
// <copyright file="PageableRequest.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dto.Requests;

/// <summary>
/// Базовая модель для Request-а с пагинацией.
/// </summary>
public abstract record PageableRequest<TFilter>
{
    /// <summary>
    /// Номер страницы.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Размер страницы.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Фильтр.
    /// </summary>
    public TFilter Filter { get; init; }

    /// <summary>
    /// Настройки сортировки.
    /// </summary>
    public List<string>? SortOptions { get; init; }
}
