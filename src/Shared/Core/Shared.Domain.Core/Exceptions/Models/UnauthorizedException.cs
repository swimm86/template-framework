// ----------------------------------------------------------------------------------------------
// <copyright file="UnauthorizedException.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Exceptions.Models;

/// <summary>
/// Исключение для ошибки аутентификации.
/// </summary>
public class UnauthorizedException : Exception
{
    /// <summary>
    /// Контекст запроса.
    /// </summary>
    public ClientRequestContext Context { get; }

    /// <summary>
    /// Инициализация <see cref="UnauthorizedException"/>.
    /// </summary>
    /// <param name="context">Контекст запроса.</param>
    public UnauthorizedException(ClientRequestContext context)
        : base($"Запрос к клиенту '{context.ClientName}' по адресу '{context.AbsolutePath}' не аутентифицирован.")
    {
        Context = context;
    }
}
