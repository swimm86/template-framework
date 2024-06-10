// ----------------------------------------------------------------------------------------------
// <copyright file="ExceptionHandler.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Exceptions.Models;
using Shared.Application.Core.Exceptions.Models.Base;

namespace Shared.Application.Core.Exceptions;

/// <summary>
/// Обработчик ошибок.
/// </summary>
/// <param name="logger"> Логгер. </param>
internal sealed class ExceptionHandler(ILogger<ExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>
    /// Обработка ошибки.
    /// </summary>
    /// <param name="httpContext"> Http контекст. </param>
    /// <param name="exception"> Ошибка. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> Результат выполнения. </returns>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var message = exception.InnerException == null
            ? exception.Message
            : $"{exception.Message} {exception.InnerException.Message}";
        logger.LogError(exception, "Возникла ошибка: {message}", message);

        var response = CreateResponseFromException(exception);
        httpContext.Response.StatusCode = response.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response
            .WriteAsJsonAsync(response, cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    private static ProblemDetails CreateResponseFromException(Exception exception)
    {
        return exception switch
        {
            AppException appException => CreateResponseFromAppException(appException),
            ValidationException validationException => CreateResponseFromValidationException(validationException),
            _ => CreateResponseFromDefaultException(),
        };
    }

    private static ProblemDetails CreateResponseFromAppException(AppException appException)
    {
        return appException switch
        {
            BusinessLogicException businessLogicException => CreateResponseFromBusinessLogicException(businessLogicException),
            NotFoundException notFoundException => CreateResponseFromNotFoundException(notFoundException),
            _ => throw new NotImplementedException(),
        };
    }

    private static ProblemDetails CreateResponseFromBusinessLogicException(BusinessLogicException businessLogicException)
        => CreateProblemDetails("Ошибка бизнес-логики.", StatusCodes.Status422UnprocessableEntity, businessLogicException.Message);

    private static ProblemDetails CreateResponseFromNotFoundException(NotFoundException notFoundException)
        => CreateProblemDetails("Ошибка - не найден.", StatusCodes.Status404NotFound, notFoundException.Message);

    private static ProblemDetails CreateResponseFromValidationException(ValidationException validationException)
        => CreateProblemDetails(
            "Ошибка валидации.", ValidationErrorsToExtensions(validationException.Errors), StatusCodes.Status400BadRequest);

    private static ProblemDetails CreateResponseFromDefaultException()
        => CreateProblemDetails("Ошибка сервера.");

    private static ProblemDetails CreateProblemDetails(
        string title, int statusCode = StatusCodes.Status500InternalServerError, string? detail = null)
        => new()
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };

    private static ProblemDetails CreateProblemDetails(
        string title, IDictionary<string, object?> extensions, int statusCode = StatusCodes.Status500InternalServerError, string? detail = null)
    {
        var problemDetails = CreateProblemDetails(title, statusCode, detail);
        problemDetails.Extensions = extensions;

        return problemDetails;
    }

    private static Dictionary<string, object?> ValidationErrorsToExtensions(IEnumerable<ValidationFailure> validationFailures)
        => new() { { "details", validationFailures.Select(x => x.ErrorMessage) } };
}