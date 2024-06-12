// ----------------------------------------------------------------------------------------------
// <copyright file="PersonsController.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Getter.Api.Controllers.Base;
using Gpn.Template.Getter.Application.Interfaces;

namespace Gpn.Template.Getter.Api.Controllers;

/// <summary>
/// Контроллер инвестиционного плана
/// </summary>
public sealed class PersonsController(
    IPersonsService personsService,
    ILogger<PersonsController> logger
    ) : GetterControllerBase(logger)
{
    /// <summary>
    /// Получение страницы инвестиционных планов
    /// </summary>
    /// <returns>Страница инвестиционных планов</returns>
    [HttpPost("list")]
    public IActionResult GetAsync()
    {
        return Ok(personsService.GetPersons());
    }
}
