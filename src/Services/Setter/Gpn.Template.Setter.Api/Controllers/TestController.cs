// ----------------------------------------------------------------------------------------------
// <copyright file="TestController.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Setter.Api.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Gpn.Template.Setter.Api.Controllers;

/// <summary>
/// Контроллер инвестиционного плана
/// </summary>
/// <param name="logger">Логгер.</param>
public sealed class TestController(ILogger<TestController> logger) : SetterControllerBase(logger)
{
    /// <summary>
    /// Получение страницы инвестиционных планов
    /// </summary>
    /// <returns>Страница инвестиционных планов</returns>
    [HttpPost("list")]
    public IActionResult GetAsync()
    {
        return default;
    }
}
