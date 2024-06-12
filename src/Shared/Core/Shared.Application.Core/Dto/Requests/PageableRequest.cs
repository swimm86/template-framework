// ----------------------------------------------------------------------------------------------
// <copyright file="PageableRequest.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dto.Requests;

/// <summary>
/// Базовая модель для Request-а с пагинацией.
/// </summary>
public abstract record PageableRequest
{
    /// <summary>
    /// Номер страницы.
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// Размер страницы.
    /// </summary>
    public int PageSize { get; init; }
}
