// ----------------------------------------------------------------------------------------------
// <copyright file="ControllerBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Dto.Responses;
using Shared.Common.Extensions;

namespace Shared.Presentation.Core.Controllers;

/// <summary>
/// Базовый класс для контроллеров
/// </summary>
/// <summary>
/// Абстрактный базовый класс для всех контроллеров, обеспечивающий общую логику обработки запросов.
/// </summary>
[ApiController]
[Route("api/pir/[controllerType]/v1/[controller]")]
public abstract class ControllerBase(ILogger logger)
    : Microsoft.AspNetCore.Mvc.ControllerBase
{
    /// <summary>
    /// Асинхронно обрабатывает запрос, используя предоставленную функцию обработки.
    /// </summary>
    /// <param name="processFunc">Функция обработки запроса, которая должна вернуть результат обработки в виде объекта <see cref="Task{TResponse}"/>, где TResponse является ответом с типизированным содержимым.</param>
    /// <param name="cancellationToken">Токен отмены операции, позволяющий отменить асинхронную операцию.</param>
    /// <param name="methodName">Наименование метода, вызвавшего асинхронную операцию.</param>
    /// <typeparam name="TResponse">Тип ответа, который должен наследоваться от <see cref="Response{TPayload}"/>, где TPayload - тип данных в теле ответа.</typeparam>
    /// <returns>Объект <see cref="Task{IActionResult}"/>, представляющий асинхронную операцию. Результат содержит статус код и данные ответа, упакованные в соответствующий формат HTTP-ответа.</returns>
    protected Task<IActionResult> Process<TResponse>(
        Func<Task<TResponse>> processFunc,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string? methodName = null)
        where TResponse : Response
    {
        return logger.LogTaskAsync(
            async () =>
            {
                var result = await processFunc()
                    .ConfigureAwait(false);
                return StatusCode(result.StatusCode, result) as IActionResult;
            },
            cancellationToken,
            methodName);
    }
}
