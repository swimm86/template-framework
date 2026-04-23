// ----------------------------------------------------------------------------------------------
// <copyright file="PersonsCqrsController.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Getter.Api.Controllers.Base;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Requests;
using Gpn.Template.Getter.Application.Features.PersonFeature.Cqrs.Queries;
using MediatR;

namespace Gpn.Template.Getter.Api.Controllers;

/// <summary>
/// Person контроллер с использованием CQRS.
/// </summary>
public sealed class PersonsCqrsController(
    ISender sender,
    ILogger<PersonsController> logger)
    : GetterControllerBase(logger)
{
    /// <summary>
    /// Возвращает список всех 'Person'-ов.
    /// </summary>
    /// <param name="request">DTO.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Список всех 'Person'-ов</returns>
    [HttpPost("list")]
    public Task<IActionResult> GetPersonsAsync(
        [FromBody] PersonListRequest request,
        CancellationToken cancellationToken = default)
    {
        return Process(
            () => sender.Send(new PersonReadListQuery(request), cancellationToken));
    }
}
