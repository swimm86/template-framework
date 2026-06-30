// ----------------------------------------------------------------------------------------------
// <copyright file="TestController.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Core.Dto.Responses;
using Template.Setter.Api.Controllers.Base;
using Template.Setter.Application.Abstractions.Features.Test.ExceptionChain;

namespace Template.Setter.Api.Controllers;

/// <summary>
/// Контроллер, предоставляющий витрину для тестирования функциональности.
/// </summary>
public sealed class TestController(
    ISender sender,
    ILogger<TestController> logger)
    : SetterControllerBase(logger)
{
    /// <summary>
    /// Проксирует вызов <c>POST /test/exception-chain</c> на Getter для проверки проброса
    /// <see cref="Shared.Application.Core.Exceptions.Models.ProxiedException"/> через HTTP-границу сервисов.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Endpoint формирует <see cref="TestExceptionChainCommand"/> и через MediatR-конвейер
    /// вызывает <see cref="Template.Setter.Application.Interfaces.HttpClients.IGetterClient.TestExceptionChainAsync"/>.
    /// Getter всегда бросает исключение, которое через <c>ProxiedResponseValidator</c>
    /// превращается в <see cref="Shared.Application.Core.Exceptions.Models.ProxiedException"/>
    /// с сохранённым <c>innerException</c>.
    /// </para>
    /// <para>
    /// Endpoint всегда возвращает 500 Internal Server Error с телом <see cref="ErrorResponse"/>,
    /// содержащим <see cref="ProblemDetails"/> от upstream-сервиса.
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>
    /// <see cref="IActionResult"/> с HTTP-статус-кодом 500 и телом <see cref="ErrorResponse"/>.
    /// </returns>
    /// <response code="500">
    /// Удалённый сервис вернул ошибку либо сбой в цепочке вызовов.
    /// Тело ответа: <see cref="ErrorResponse"/> с <see cref="ProblemDetails"/>.
    /// </response>
    [HttpPost("exception-chain")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> TestExceptionChainAsync(
        CancellationToken cancellationToken = default)
    {
        return Process(
            () => sender.Send(new TestExceptionChainCommand(), cancellationToken));
    }
}
