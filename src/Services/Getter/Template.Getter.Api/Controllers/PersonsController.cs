// ----------------------------------------------------------------------------------------------
// <copyright file="PersonsController.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;
using Template.Getter.Api.Controllers.Base;
using Template.Getter.Application.Abstractions.Features.Person.List.Request;
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
    /// <param name="request">Тело запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Коллекция сущностей "Персона".</returns>
    [HttpPost("services/list")]
    public Task<IActionResult> GetPersonsByServicesAsync(
        [FromBody] PersonListRequest request,
        CancellationToken cancellationToken = default) =>
        Process(() => personsService.GetPersonsAsync(request, cancellationToken));

    /// <summary>
    /// Возвращает коллекцию сущностей "Персона" через CQRS (MediatR).
    /// </summary>
    /// <param name="request">Тело запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Коллекция сущностей "Персона".</returns>
    [HttpPost("cqrs/list")]
    public Task<IActionResult> GetPersonsByCqrsAsync(
        [FromBody] PersonListRequest request,
        CancellationToken cancellationToken = default)
    {
        return Process(
            () => sender.Send(new PersonListQuery(request), cancellationToken));
    }
}
