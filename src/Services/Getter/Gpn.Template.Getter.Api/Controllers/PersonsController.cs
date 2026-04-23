// ----------------------------------------------------------------------------------------------
// <copyright file="PersonsController.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Getter.Api.Controllers.Base;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Requests;
using Gpn.Template.Getter.Application.Interfaces;

namespace Gpn.Template.Getter.Api.Controllers;

/// <summary>
/// Person контроллер.
/// </summary>
public sealed class PersonsController(
    IPersonsService personsService,
    ILogger<PersonsController> logger)
    : GetterControllerBase(logger)
{
    /// <summary>
    /// Возвращает список всех 'Person'-ов.
    /// </summary>
    /// <param name="dto">DTO.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Список всех 'Person'-ов</returns>
    [HttpPost("list")]
    public Task<IActionResult> GetPersonsAsync(
        [FromBody] PersonListRequest dto,
        CancellationToken cancellationToken = default) =>
        Process(() => personsService.GetPersonsAsync(dto, cancellationToken));
}
