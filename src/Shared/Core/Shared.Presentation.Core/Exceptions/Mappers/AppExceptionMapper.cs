// ----------------------------------------------------------------------------------------------
// <copyright file="AppExceptionMapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Shared.Domain.Core.Exceptions.Models.Base;
using Shared.Presentation.Core.Exceptions.Mappers.Base;

namespace Shared.Presentation.Core.Exceptions.Mappers;

/// <summary>
/// Преобразователь доменных исключений приложения <see cref="AppException"/> в ProblemDetails.
/// </summary>
/// <remarks>
/// Используется для базового типа и подтипов, для которых не зарегистрирован более специфичный преобразователь.
/// Возвращает ответ с кодом 500 и сообщением исключения.
/// </remarks>
public sealed class AppExceptionMapper(
    IConfiguration configuration)
    : AppExceptionMapperBase<AppException>(configuration)
{
    /// <inheritdoc/>
    protected override string Title => "Ошибка приложения";

    /// <inheritdoc/>
    protected override int GetResponseStatusCode(AppException exception)
        => StatusCodes.Status500InternalServerError;
}
