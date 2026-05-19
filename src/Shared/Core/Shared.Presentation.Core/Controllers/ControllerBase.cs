// ----------------------------------------------------------------------------------------------
// <copyright file="ControllerBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Dto.Responses;
using Shared.Common.Logging.Extensions;

namespace Shared.Presentation.Core.Controllers;

/// <summary>
/// Абстрактный базовый класс для всех контроллеров, обеспечивающий общую логику обработки запросов.
/// </summary>
/// <remarks>
/// <para>
/// Предоставляет единую точку входа для обработки запросов с автоматическим
/// логированием, обработкой исключений и формированием HTTP-ответа.
/// </para>
/// <para>
/// <b>Архитектурная роль:</b>
/// <list type="bullet">
/// <item>Инкапсулирует обработку CQRS-запросов через метод <see cref="Process{TResponse}"/></item>
/// <item>Автоматически логирует выполнение запросов через <see cref="ILogger"/></item>
/// <item>Преобразует результаты в HTTP-ответы с соответствующими статус кодами</item>
/// </list>
/// </para>
/// </remarks>
/// <param name="logger">Логгер для записи событий выполнения запросов.</param>
[ApiController]
[Route("api/[appName]/[controllerType]/v1/[controller]")]
public abstract class ControllerBase(
    ILogger logger)
    : Microsoft.AspNetCore.Mvc.ControllerBase
{
    /// <summary>
    /// Асинхронно обрабатывает запрос и возвращает HTTP-ответ с соответствующим статус-кодом.
    /// </summary>
    /// <typeparam name="TResponse">Тип ответа, наследующий <see cref="Response"/>.</typeparam>
    /// <param name="processFunc">Функция обработки запроса.</param>
    /// <param name="methodName">Имя вызвавшего метода (заполняется автоматически).</param>
    /// <returns>Результат выполнения запроса в виде <see cref="IActionResult"/>.</returns>
    protected Task<IActionResult> Process<TResponse>(
        Func<Task<TResponse>> processFunc,
        [CallerMemberName] string? methodName = null)
        where TResponse : Response
    {
        return logger.LogTaskAsync(
            async () =>
            {
                var result = await processFunc();
                return StatusCode(result.StatusCode, result) as IActionResult;
            },
            methodName);
    }
}
