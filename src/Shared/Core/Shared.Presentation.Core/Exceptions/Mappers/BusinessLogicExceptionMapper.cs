// ----------------------------------------------------------------------------------------------
// <copyright file="BusinessLogicExceptionMapper.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Presentation.Core.Exceptions.Mappers.Base;

namespace Shared.Presentation.Core.Exceptions.Mappers;

/// <summary>
/// Маппер ошибок бизнес-логики <see cref="BusinessLogicException"/> в ProblemDetails.
/// </summary>
/// <remarks>
/// Возвращает RFC 7807 Problem Details с кодом 422 Unprocessable Entity и сообщением исключения.
/// </remarks>
public sealed class BusinessLogicExceptionMapper(
    IConfiguration configuration)
    : AppExceptionMapperBase<BusinessLogicException>(configuration)
{
    /// <inheritdoc/>
    protected override string Title => "Ошибка бизнес-логики";

    /// <inheritdoc/>
    protected override int GetResponseStatusCode(BusinessLogicException exception)
        => StatusCodes.Status422UnprocessableEntity;
}
