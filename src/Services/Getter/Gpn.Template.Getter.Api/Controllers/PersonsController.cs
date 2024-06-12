// ----------------------------------------------------------------------------------------------
// <copyright file="PersonsController.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Getter.Api.Controllers.Base;
using Gpn.Template.Getter.Application.Interfaces;
using Gpn.Template.Getter.Application.Requests;

namespace Gpn.Template.Getter.Api.Controllers;

/// <summary>
/// Person контроллер.
/// </summary>
public sealed class PersonsController(
    IPersonsService personsService,
    ILogger<PersonsController> logger
    ) : GetterControllerBase(logger)
{
    /// <summary>
    /// Возвращает список всех 'Person'-ов.
    /// </summary>
    /// <returns>Список всех 'Person'-ов</returns>
    [HttpPost("list")]
    public IActionResult GetPersonsAsync([FromBody] GetPersonsRequestDto dto)
    {
        return Ok(personsService.GetPersons(dto));
    }
}
