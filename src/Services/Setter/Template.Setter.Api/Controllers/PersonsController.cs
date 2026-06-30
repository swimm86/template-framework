// ----------------------------------------------------------------------------------------------
// <copyright file="PersonsController.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Core.Dto.Responses;
using Template.Setter.Api.Controllers.Base;
using Template.Setter.Application.Abstractions.Features.Person.Create;
using Template.Setter.Application.Abstractions.Features.Person.Create.Request;
using Template.Setter.Application.Abstractions.Features.Person.Create.Response;

namespace Template.Setter.Api.Controllers;

/// <summary>
/// Контроллер для взаимодействия с сущностями 'Персона'.
/// </summary>
public sealed class PersonsController(
    ISender sender,
    ILogger<PersonsController> logger)
    : SetterControllerBase(logger)
{
    /// <summary>
    /// Создаёт сущность "Персона".
    /// </summary>
    /// <remarks>
    /// Запрос делегируется в CQRS-конвейер: <c>PersonCreateCommand</c> → <c>PersonCreateCommandHandler</c>,
    /// который наследует <c>CreateCommandHandler</c> и выполняет:
    /// <list type="number">
    /// <item>Guard-проверку запроса.</item>
    /// <item>Маппинг DTO <paramref name="request"/> в доменную сущность через <c>IMapper</c>.</item>
    /// <item>Запуск lifecycle actions (<c>ProcessEntityAsync</c>) — установка аудит-полей, хэша и т.п.</item>
    /// <item>Валидацию через <c>FluentValidation</c>.</item>
    /// <item>Сохранение через <c>Repository.AddAsync</c> + <c>UnitOfWork.SaveChangesAsync</c>.</item>
    /// </list>
    /// HTTP-статус результата всегда <c>201 Created</c>.
    /// </remarks>
    /// <param name="request">Запрос с данными для создания сущности "Персона".</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>
    /// <see cref="IActionResult"/> с HTTP-статус-кодом 201 и телом <see cref="PersonCreateResponse"/>
    /// (идентификатор и полезная нагрузка созданной сущности) либо <see cref="ErrorResponse"/> при ошибке.
    /// </returns>
    /// <response code="201">Сущность успешно создана. Тело ответа: <see cref="PersonCreateResponse"/>.</response>
    /// <response code="400">
    /// Некорректный запрос или нарушены FluentValidation-правила.
    /// Тело ответа: <see cref="ValidationProblemDetails"/>.
    /// </response>
    /// <response code="500">
    /// Внутренняя ошибка сервера. Тело ответа: <see cref="ErrorResponse"/> с <see cref="ProblemDetails"/>.
    /// </response>
    [HttpPost("create")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PersonCreateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> PersonsCreateAsync(
        [FromBody] PersonCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        return Process(
            () => sender.Send(new PersonCreateCommand(request), cancellationToken));
    }
}
