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
/// <param name="Errors">Ошибки.</param>
public record ErrorResponse(ICollection<ProblemDetails> Errors) : ResponseBase
{
    /// <summary>
    /// Подробная информация об ошибке.
    /// </summary>
    public string? Details { get; internal set; }
}
