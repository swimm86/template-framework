// ----------------------------------------------------------------------------------------------
// <copyright file="ProxiedException.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;

namespace Shared.Application.Core.Exceptions;

/// <summary>
/// Проксированная ошибка.
/// </summary>
public class ProxiedException : Exception
{
    /// <summary>
    /// Детали ошибки.
    /// </summary>
    public readonly ProblemDetails ProblemDetails;

    /// <summary>
    /// Статус ответа.
    /// </summary>
    public readonly int StatusCode;

    /// <summary>
    /// Инициализация <see cref="ProxiedException"/>.
    /// </summary>
    /// <param name="problemDetails">Детали ошибки.</param>
    /// <param name="statusCode">Статус ответа.</param>
    public ProxiedException(ProblemDetails problemDetails, int statusCode)
    {
        ProblemDetails = problemDetails;
        StatusCode = statusCode;
    }
}
