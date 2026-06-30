// ----------------------------------------------------------------------------------------------
// <copyright file="UnauthorizedException.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Exceptions.Models;
using Shared.Domain.Core.Exceptions.Models.Base;

namespace Shared.Presentation.Core.Exceptions.Models;

/// <summary>
/// Исключение, возникающее при ошибке аутентификации при обращении к внешнему сервису (HTTP 401 Unauthorized).
/// </summary>
public class UnauthorizedException
    : AppException
{
    /// <summary>
    /// Контекст запроса, содержащий информацию о клиенте и запрашиваемом ресурсе.
    /// </summary>
    public ClientRequestContext Context { get; }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="UnauthorizedException"/>.
    /// </summary>
    /// <param name="context">Контекст запроса, содержащий данные о клиенте и домене.</param>
    /// <inheritdoc cref="AppException(string, Exception?, IReadOnlyDictionary{string, object}?)"/>
    /// <param name="innerException"/><param name="additionalData"/>
    public UnauthorizedException(
        ClientRequestContext context,
        Exception? innerException = null,
        IReadOnlyDictionary<string, object>? additionalData = null)
        : base(
            $"Запрос к клиенту '{context.ClientName}' по адресу '{context.AbsolutePath}' не аутентифицирован",
            innerException,
            additionalData)
    {
        Context = context;
    }
}
