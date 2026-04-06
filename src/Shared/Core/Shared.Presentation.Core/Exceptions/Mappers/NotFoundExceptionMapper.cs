// ----------------------------------------------------------------------------------------------
// <copyright file="NotFoundExceptionMapper.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Presentation.Core.Exceptions.Mappers.Base;

namespace Shared.Presentation.Core.Exceptions.Mappers;

/// <summary>
/// Маппер исключений «сущность не найдена» <see cref="NotFoundException"/> в ProblemDetails.
/// </summary>
/// <remarks>
/// Возвращает RFC 7807 Problem Details с кодом 404 Not Found и сообщением исключения.
/// </remarks>
public sealed class NotFoundExceptionMapper(
    IConfiguration configuration)
    : AppExceptionMapperBase<NotFoundException>(configuration)
{
    /// <inheritdoc/>
    protected override string Title => "Ошибка - не найден";

    /// <inheritdoc/>
    protected override int GetResponseStatusCode(NotFoundException exception)
        => StatusCodes.Status404NotFound;
}
