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
    /// <param name="dto">DTO.</param>
    /// <returns>Список всех 'Person'-ов</returns>
    [HttpPost("list")]
    public async Task<IActionResult> GetPersonsAsync([FromBody] GetPersonsRequestDto dto)
    {
        return Ok(await personsService.GetPersonsAsync(dto).ConfigureAwait(false));
    }
}
