// ----------------------------------------------------------------------------------------------
// <copyright file="ValidationExceptionMapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Shared.Presentation.Core.Exceptions.Mappers.Base;

namespace Shared.Presentation.Core.Exceptions.Mappers;

/// <summary>
/// Преобразователь ошибок валидации FluentValidation <see cref="ValidationException"/> в <see cref="ProblemDetails"/>.
/// </summary>
/// <remarks>
/// Возвращает RFC 7807 Problem Details с кодом 400 Bad Request.
/// Уникальные сообщения ошибок валидации объединяются в <see cref="ProblemDetails.Detail"/>
/// через разделитель `;` для единообразной обработки фронтендом.
/// </remarks>
public sealed class ValidationExceptionMapper(
    IConfiguration configuration)
    : ExceptionMapperBase<ValidationException>(configuration)
{
    /// <inheritdoc/>
    protected override string Title => "Ошибка валидации";

    /// <inheritdoc/>
    protected override int GetResponseStatusCode(ValidationException exception)
        => StatusCodes.Status400BadRequest;

    /// <inheritdoc/>
    protected override IReadOnlyCollection<ProblemDetails> GetProblemDetails(
        ValidationException exception)
    {
        var details = exception.Errors.Any()
            ? string.Join(
                $";{Environment.NewLine}",
                exception.Errors
                    .Select(x => x.ErrorMessage)
                    .Distinct())
            : null;
        return
        [
            new ProblemDetails
            {
                Status = GetResponseStatusCode(exception),
                Title = Title,
                Detail = details,
            },
        ];
    }
}
