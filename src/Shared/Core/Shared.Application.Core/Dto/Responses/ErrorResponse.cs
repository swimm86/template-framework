// ----------------------------------------------------------------------------------------------
// <copyright file="ErrorResponse.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Dto.Responses;

/// <summary>
/// Ответ с ошибками.
/// </summary>
public record ErrorResponse
    : ResponseBase, IWithAdditionalData
{
    /// <summary>
    /// Ошибки.
    /// </summary>
    public IReadOnlyCollection<ProblemDetails> Errors { get; init; }

    /// <summary>
    /// Подробная информация об ошибке.
    /// </summary>
    public string? Details { get; init; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object>? AdditionalData { get; init; }
}
