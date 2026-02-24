// ----------------------------------------------------------------------------------------------
// <copyright file="ErrorResponse.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;

namespace Shared.Application.Core.Dto.Responses;

/// <summary>
/// Ответ с ошибками.
/// </summary>
public record ErrorResponse
    : ResponseBase
{
    /// <summary>
    /// Ошибки.
    /// </summary>
    public ICollection<ProblemDetails> Errors { get; set; }

    /// <summary>
    /// Подробная информация об ошибке.
    /// </summary>
    public string? Details { get; internal set; }

    /// <summary>
    /// Дополнительная информация.
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }
}
