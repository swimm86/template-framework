// ----------------------------------------------------------------------------------------------
// <copyright file="PersonController.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Core.Dto.Responses;
using Template.Bff.Api.Controllers.Base;
using Template.Bff.Application.Features.Queries.Person.Cqrs.List;
using Template.Bff.Application.Features.Queries.Person.Cqrs.List.Requests;
using Template.Getter.Application.Abstractions.Features.Person.List.Response;
using Template.Setter.Application.Abstractions.Features.Person.Create;
using Template.Setter.Application.Abstractions.Features.Person.Create.Request;
using Template.Setter.Application.Abstractions.Features.Person.Create.Response;

namespace Template.Bff.Api.Controllers;

/// <summary>
/// Контроллер для взаимодействия с сущностями "Персона".
/// </summary>
public class PersonController(
    ISender sender,
    ILogger<PersonController> logger)
    : BffControllerBase(logger)
{
    /// <summary>
    /// Возвращает коллекцию сущностей "Персона".
    /// </summary>
    /// <remarks>
    /// Запрос делегируется в CQRS-конвейер: <c>PersonListQuery</c> → <c>PersonListQueryHandler</c>,
    /// который вызывает <see cref="Template.Bff.Application.Interfaces.HttpClients.IGetterClient.GetPersonsAsync"/>.
    /// Используемый паттерн определяется значением <c>UseCqrs</c> в <paramref name="request"/>:
    /// <c>true</c> → <c>GetPersonsPattern.Cqrs</c> (POST <c>persons/cqrs/list</c>),
    /// <c>false</c> → <c>GetPersonsPattern.Services</c> (POST <c>persons/services/list</c>).
    /// </remarks>
    /// <param name="request">Параметры запроса (<c>DalPattern</c>, <c>UseCqrs</c>, пагинация, фильтры, сортировка).</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>
    /// <see cref="IActionResult"/> с HTTP-статус-кодом и телом <see cref="PersonListResponse"/>
    /// (страница сущностей) либо <see cref="ErrorResponse"/> при ошибке.
    /// </returns>
    /// <response code="200">Коллекция успешно получена. Тело ответа: <see cref="PersonListResponse"/>.</response>
    /// <response code="204">Коллекция пуста. Тело ответа отсутствует.</response>
    /// <response code="400">
    /// Некорректный запрос. Тело ответа: <see cref="ValidationProblemDetails"/>.
    /// </response>
    /// <response code="500">
    /// Внутренняя ошибка сервера или ошибка upstream-сервиса.
    /// Тело ответа: <see cref="ErrorResponse"/> с <see cref="ProblemDetails"/>.
    /// </response>
    [HttpPost("list")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PersonListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPersonsListAsync(
        PersonListRequest request,
        CancellationToken cancellationToken = default)
    {
        return Process(
            () => sender.Send(new PersonListQuery(request), cancellationToken));
    }

    /// <summary>
    /// Создаёт сущность "Персона".
    /// </summary>
    /// <remarks>
    /// Запрос делегируется в CQRS-конвейер: <c>PersonCreateCommand</c> → <c>PersonCreateCommandHandler</c>,
    /// который вызывает <see cref="Template.Bff.Application.Interfaces.HttpClients.ISetterClient.CreatePersonAsync"/>
    /// (POST <c>persons/create</c> на Setter).
    /// </remarks>
    /// <param name="request">Данные для создания сущности "Персона".</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>
    /// <see cref="IActionResult"/> с HTTP-статус-кодом 201 и телом <see cref="PersonCreateResponse"/>
    /// (идентификатор и полезная нагрузка созданной сущности) либо <see cref="ErrorResponse"/> при ошибке.
    /// </returns>
    /// <response code="201">Сущность успешно создана. Тело ответа: <see cref="PersonCreateResponse"/>.</response>
    /// <response code="400">
    /// Некорректный запрос. Тело ответа: <see cref="ValidationProblemDetails"/>.
    /// </response>
    /// <response code="500">
    /// Внутренняя ошибка сервера или ошибка upstream-сервиса.
    /// Тело ответа: <see cref="ErrorResponse"/> с <see cref="ProblemDetails"/>.
    /// </response>
    [HttpPost("create")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PersonCreateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> CreatePersonAsync(
        PersonCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        return Process(
            () => sender.Send(new PersonCreateCommand(request), cancellationToken));
    }
}
