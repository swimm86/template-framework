// ----------------------------------------------------------------------------------------------
// <copyright file="PersonController.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Template.Bff.Api.Controllers.Base;
using Template.Bff.Application.Features.Queries.Person.Cqrs.List;
using Template.Bff.Application.Features.Queries.Person.Cqrs.List.Requests;
using Template.Setter.Application.Abstractions.Features.Person.Create;
using Template.Setter.Application.Abstractions.Features.Person.Create.Request;

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
    /// Возвращает коллекцию сущностей"Персона".
    /// </summary>
    /// <param name="request">Тело запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Коллекция сущностей "Персона".</returns>
    [HttpPost("person/list")]
    public Task<IActionResult> GetPersonsListAsync(
        PersonListRequest request,
        CancellationToken cancellationToken = default)
    {
        return Process(
            () => sender.Send(new PersonListQuery(request), cancellationToken));
    }

    /// <summary>
    /// Создает сущность "Персона".
    /// </summary>
    /// <param name="request">Тело запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Информация о созданной сущности "Персона".</returns>
    [HttpPost("person/create")]
    public Task<IActionResult> CreatePersonAsync(
        PersonCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        return Process(
            () => sender.Send(new PersonCreateCommand(request), cancellationToken));
    }
}
