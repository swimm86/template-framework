// ----------------------------------------------------------------------------------------------
// <copyright file="PersonsCqrsController.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Getter.Api.Controllers.Base;
using Gpn.Template.Getter.Application.Features.PersonFeature.Cqrs.Queries;
using Gpn.Template.Getter.Application.Features.PersonFeature.Dtos.Requests;
using MediatR;

namespace Gpn.Template.Getter.Api.Controllers;

/// <summary>
/// Person контроллер с использованием CQRS.
/// </summary>
public sealed class PersonsCqrsController(
    ISender sender,
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
        return Ok(await sender.Send(new PersonReadListQuery(dto)).ConfigureAwait(false));
    }
}
