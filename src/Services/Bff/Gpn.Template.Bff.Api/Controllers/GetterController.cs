// ----------------------------------------------------------------------------------------------
// <copyright file="GetterController.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Api.Controllers.Base;
using Gpn.Template.Bff.Api.Interfaces;
using Gpn.Template.Bff.Application.Features.Person.Cqrs.Queries.List;
using Gpn.Template.Bff.Application.Features.Person.Cqrs.Queries.List.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gpn.Template.Bff.Api.Controllers;

/// <summary>
/// Контроллер для взаимодействия с сервисом "Getter".
/// </summary>
public class GetterController(
    ISender sender,
    ILogger<GetterController> logger)
    : BffControllerBase(logger), IGetterController
{
    /// <summary>
    /// Возвращает коллекцию сущностей 'Person'.
    /// </summary>
    /// <param name="request">Тело запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Коллекция сущностей 'Person'.</returns>
    [HttpPost("person/list")]
    public Task<IActionResult> GetPersonsAsync(
        PersonListRequest request,
        CancellationToken cancellationToken = default)
    {
        return Process(
            () => sender.Send(new PersonListQuery(request), cancellationToken));
    }
}
