// ----------------------------------------------------------------------------------------------
// <copyright file="DefaultExceptionMapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Shared.Presentation.Core.Exceptions.Mappers.Base;

namespace Shared.Presentation.Core.Exceptions.Mappers;

/// <summary>
/// Преобразователь по умолчанию для любого необработанного <see cref="Exception"/>.
/// </summary>
/// <remarks>
/// Используется резолвером, когда для типа исключения не зарегистрирован более специфичный преобразователь.
/// Возвращает RFC 7807 Problem Details с кодом 500 Internal Server Error и сообщением исключения.
/// </remarks>
public sealed class DefaultExceptionMapper(
    IConfiguration configuration)
    : ExceptionMapperBase<Exception>(configuration)
{
    /// <inheritdoc/>
    protected override string Title => "Ошибка сервера";

    /// <inheritdoc/>
    protected override int GetResponseStatusCode(Exception exception)
        => StatusCodes.Status500InternalServerError;
}
