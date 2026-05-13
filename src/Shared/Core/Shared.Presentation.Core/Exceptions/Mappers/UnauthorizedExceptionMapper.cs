// ----------------------------------------------------------------------------------------------
// <copyright file="UnauthorizedExceptionMapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Shared.Presentation.Core.Exceptions.Mappers.Base;
using Shared.Presentation.Core.Exceptions.Models;

namespace Shared.Presentation.Core.Exceptions.Mappers;

/// <summary>
/// Маппер исключений неаутентифицированного доступа <see cref="UnauthorizedException"/> в ProblemDetails.
/// </summary>
/// <remarks>
/// Возвращает RFC 7807 Problem Details с кодом 401 Unauthorized.
/// </remarks>
public sealed class UnauthorizedExceptionMapper(
    IConfiguration configuration)
    : AppExceptionMapperBase<UnauthorizedException>(configuration)
{
    /// <inheritdoc/>
    protected override string Title => "Пользователь не аутентифицирован";

    /// <inheritdoc/>
    protected override int GetResponseStatusCode(UnauthorizedException exception)
        => StatusCodes.Status401Unauthorized;
}
