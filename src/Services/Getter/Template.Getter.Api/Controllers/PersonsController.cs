// ----------------------------------------------------------------------------------------------
// <copyright file="PersonsController.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Core.Dto.Responses;
using Template.Getter.Api.Controllers.Base;
using Template.Getter.Application.Abstractions.Features.Person.List.Request;
using Template.Getter.Application.Abstractions.Features.Person.List.Response;
using Template.Getter.Application.Features.Person.Cqrs.List;
using Template.Getter.Application.Interfaces;

namespace Template.Getter.Api.Controllers;

/// <summary>
/// Контроллер для взаимодействия с сущностями "Персона".
/// </summary>
public sealed class PersonsController(
    IPersonsService personsService,
    ISender sender,
    ILogger<PersonsController> logger)
    : GetterControllerBase(logger)
{
    /// <summary>
    /// Возвращает коллекцию сущностей "Персона" через слой приложения (без CQRS).
    /// </summary>
    /// <remarks>
    /// Запрос обрабатывается напрямую через <see cref="IPersonsService.GetPersonsAsync"/>,
    /// минуя MediatR-конвейер. <paramref name="request"/>.DalPattern выбирает реализацию доступа к данным
    /// (<c>UnitOfWork</c> / <c>Repository</c> / <c>Specification</c>).
    /// HTTP-статус ответа зависит от количества результатов: 200 при наличии элементов, 204 при пустой коллекции.
    /// </remarks>
    /// <param name="request">Параметры запроса (<c>DalPattern</c>, пагинация, фильтры, сортировка).</param>
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
    /// Внутренняя ошибка сервера. Тело ответа: <see cref="ErrorResponse"/> с <see cref="ProblemDetails"/>.
    /// </response>
    [HttpPost("services/list")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PersonListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPersonsByServicesAsync(
        [FromBody] PersonListRequest request,
        CancellationToken cancellationToken = default) =>
        Process(() => personsService.GetPersonsAsync(request, cancellationToken));

    /// <summary>
    /// Возвращает коллекцию сущностей "Персона" через CQRS (MediatR).
    /// </summary>
    /// <remarks>
    /// Запрос делегируется в CQRS-конвейер: <c>PersonListQuery</c> → <c>PersonListQueryHandler</c>.
    /// <paramref name="request"/>.DalPattern выбирает реализацию доступа к данным
    /// (<c>UnitOfWork</c> / <c>Repository</c> / <c>Specification</c>).
    /// HTTP-статус ответа зависит от количества результатов: 200 при наличии элементов, 204 при пустой коллекции.
    /// </remarks>
    /// <param name="request">Параметры запроса (<c>DalPattern</c>, пагинация, фильтры, сортировка).</param>
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
    /// Внутренняя ошибка сервера. Тело ответа: <see cref="ErrorResponse"/> с <see cref="ProblemDetails"/>.
    /// </response>
    [HttpPost("cqrs/list")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PersonListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPersonsByCqrsAsync(
        [FromBody] PersonListRequest request,
        CancellationToken cancellationToken = default)
    {
        return Process(
            () => sender.Send(new PersonListQuery(request), cancellationToken));
    }
}
