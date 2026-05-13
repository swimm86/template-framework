// ----------------------------------------------------------------------------------------------
// <copyright file="ExceptionHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Shared.Application.Core.Dto.Responses;
using Shared.Presentation.Core.Exceptions.Interfaces;

namespace Shared.Presentation.Core.Exceptions;

/// <summary>
/// Обработчик необработанных исключений приложения.
/// </summary>
/// <remarks>
/// Перехватывает исключения, преобразует их в <see cref="ErrorResponse"/>
/// и записывает в HTTP-ответ с соответствующим статус-кодом.
/// </remarks>
/// <param name="exceptionMapperDispatcher">Резолвер маппера по типу исключения.</param>
internal sealed class ExceptionHandler(
    IExceptionMapperResolver exceptionMapperDispatcher)
    : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var response = exceptionMapperDispatcher.Map(exception);
        httpContext.Response.StatusCode = response.StatusCode;

        await httpContext.Response
            .WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
