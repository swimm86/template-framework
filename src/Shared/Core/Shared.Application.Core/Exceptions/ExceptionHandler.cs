// ----------------------------------------------------------------------------------------------
// <copyright file="ExceptionHandler.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Application.Core.Dto.Responses;
using Shared.Application.Core.Exceptions.Settings;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Domain.Core.Exceptions.Models.Base;

namespace Shared.Application.Core.Exceptions;

/// <summary>
/// Обработчик ошибок.
/// </summary>
/// <param name="logger">Логгер.</param>
/// <param name="configuration">Конфигурация.</param>
internal sealed class ExceptionHandler(
    ILogger<ExceptionHandler> logger,
    IConfiguration configuration)
    : IExceptionHandler
{
    private readonly bool _isDebug = configuration.GetOptions<RichDebugSettings>()?.IsEnabled ?? false;

    /// <summary>
    /// Обработка ошибки.
    /// </summary>
    /// <param name="httpContext"> Http контекст. </param>
    /// <param name="exception"> Ошибка. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> Результат выполнения. </returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var message = exception.InnerException == null
            ? exception.Message
            : $"{exception.Message} {exception.InnerException.Message}";
        logger.LogError(exception, "Возникла ошибка: {message}", message);

        var response = CreateResponseFromException(exception);
        httpContext.Response.StatusCode = response.StatusCode;

        await httpContext.Response
            .WriteAsJsonAsync(response, cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    private static ICollection<ProblemDetails> ProcessException(
        Exception exception)
    {
        var details = exception switch
        {
            AppException appException => CreateResponseFromAppException(appException),
            ValidationException validationException => CreateResponseFromValidationException(validationException),
            UnauthorizedException unauthorizedException => CreateResponseFromUnauthorizedException(unauthorizedException),
            ProxiedException proxiedException => CreateResponseFromProxiedException(proxiedException),
            _ => CreateResponseFromDefaultException(),
        };

        return [details];
    }

    private static ProblemDetails CreateResponseFromAppException(
        AppException appException)
    {
        return appException switch
        {
            BusinessLogicException businessLogicException => CreateResponseFromBusinessLogicException(businessLogicException),
            NotFoundException notFoundException => CreateResponseFromNotFoundException(notFoundException),
            _ => CreateResponseFromApplicationException(appException),
        };
    }

    private static ProblemDetails CreateResponseFromProxiedException(
        ProxiedException proxiedException)
    {
        proxiedException.ProblemDetails.Status = proxiedException.StatusCode;
        return proxiedException.ProblemDetails;
    }

    private static ProblemDetails CreateResponseFromUnauthorizedException(
        UnauthorizedException unauthorizedException)
        => CreateProblemDetails("Пользователь не аутентифицирован", StatusCodes.Status401Unauthorized, unauthorizedException.Message);

    private static ProblemDetails CreateResponseFromBusinessLogicException(
        BusinessLogicException businessLogicException)
        => CreateProblemDetails("Ошибка бизнес-логики", StatusCodes.Status422UnprocessableEntity, businessLogicException.Message);

    private static ProblemDetails CreateResponseFromNotFoundException(
        NotFoundException notFoundException)
        => CreateProblemDetails("Ошибка - не найден", StatusCodes.Status404NotFound, notFoundException.Message);

    private static ProblemDetails CreateResponseFromValidationException(
        ValidationException validationException)
        => CreateProblemDetails(
            "Ошибка валидации", ValidationErrorsToExtensions(validationException.Errors), StatusCodes.Status400BadRequest);

    private static ProblemDetails CreateResponseFromApplicationException(
        AppException validationException)
        => CreateProblemDetails(
            "Ошибка приложения", StatusCodes.Status500InternalServerError, validationException.Message);

    private static ProblemDetails CreateResponseFromDefaultException()
        => CreateProblemDetails("Ошибка сервера");

    private static ProblemDetails CreateProblemDetails(
        string title,
        int statusCode = StatusCodes.Status500InternalServerError,
        string? detail = null)
        => new ProblemDetails()
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
        };

    private static ProblemDetails CreateProblemDetails(
        string title,
        IDictionary<string, object?> extensions,
        int statusCode = StatusCodes.Status500InternalServerError,
        string? detail = null)
    {
        var problemDetails = CreateProblemDetails(title, statusCode, detail);
        problemDetails.Extensions = extensions;

        return problemDetails;
    }

    private static Dictionary<string, object?> ValidationErrorsToExtensions(
        IEnumerable<ValidationFailure> validationFailures)
        => new()
        {
            {
                "errors",
                validationFailures
                    .Select(x => new { field = x.PropertyName, message = x.ErrorMessage })
                    .DistinctBy(x => x.field + x.message)
                    .ToArray()
            }
        };

    private ErrorResponse CreateResponseFromException(
        Exception exception)
    {
        var details = ProcessException(exception);
        var response = new ErrorResponse(details)
        {
            StatusCode = details.FirstOrDefault()?.Status ?? StatusCodes.Status500InternalServerError,
        };

        var enrichErrorResponse = false;
#if DEBUG
        enrichErrorResponse = true;
#endif

        var shouldEnrichResponseWithTrace = exception is not ProxiedException && (_isDebug || enrichErrorResponse);
        if (shouldEnrichResponseWithTrace)
        {
            response.Details = GetExceptionDetails(exception);
        }

        return response;
    }

    private string GetExceptionDetails(
        Exception exception,
        int stackTraceDepth = 2)
    {
        var detailsBuilder = new StringBuilder();

        detailsBuilder.Append(exception.GetType());
        if (!string.IsNullOrEmpty(exception.Message))
        {
            detailsBuilder.Append(": ");
            detailsBuilder.Append(exception.Message);
        }

        if (exception.InnerException is not null)
        {
            detailsBuilder.AppendLine();
            detailsBuilder.Append(" ---> ");

            var innerExceptionDetails = GetExceptionDetails(exception.InnerException, stackTraceDepth);
            detailsBuilder.Append(innerExceptionDetails);

            detailsBuilder.AppendLine();
            detailsBuilder.Append("   ");
            detailsBuilder.Append("--- End of inner exception stack trace ---");
        }

        if (exception.StackTrace is not null)
        {
            var stackTracePartLines = exception.StackTrace
                .Split(Environment.NewLine)
                .Take(stackTraceDepth);

            foreach (var stackTracePartLine in stackTracePartLines)
            {
                detailsBuilder.AppendLine();
                detailsBuilder.Append(stackTracePartLine);
            }
        }

        return detailsBuilder.ToString();
    }
}
