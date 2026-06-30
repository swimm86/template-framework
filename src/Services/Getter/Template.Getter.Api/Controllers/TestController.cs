// ----------------------------------------------------------------------------------------------
// <copyright file="TestController.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Shared.Application.Core.Dto.Responses;
using Template.Getter.Api.Controllers.Base;

namespace Template.Getter.Api.Controllers;

/// <summary>
/// Контроллер, предоставляющий витрину для тестирования функциональности.
/// </summary>
public sealed class TestController(
    ILogger<TestController> logger)
    : GetterControllerBase(logger)
{
    /// <summary>
    /// Генерирует необработанное исключение для проверки трансляции ошибок
    /// через <c>DefaultExceptionMapper</c> в HTTP-ответ 500 Internal Server Error.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Endpoint предназначен для ручной проверки того, как глобальный <c>ExceptionHandler</c>
    /// промежуточного слоя преобразует произвольное <see cref="Exception"/> в RFC 7807
    /// ответ со статус-кодом 500.
    /// </para>
    /// <para>
    /// Метод всегда бросает <see cref="Exception"/>; <see cref="IActionResult"/> в сигнатуре
    /// оставлен для совместимости с конвенцией контроллеров ASP.NET Core и для корректной
    /// генерации OpenAPI-спецификации.
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>
    /// <see cref="IActionResult"/> не возвращается — метод всегда бросает <see cref="Exception"/>.
    /// </returns>
    /// <response code="500">
    /// Всегда. Тело ответа: <see cref="ErrorResponse"/> с <see cref="ProblemDetails"/>.
    /// </response>
    [HttpPost("exception-chain")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> TestThrowAsync(
        CancellationToken cancellationToken = default)
    {
        throw new Exception("Test exception from Getter service");
    }
}
