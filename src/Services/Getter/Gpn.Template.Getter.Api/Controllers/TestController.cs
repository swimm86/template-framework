// ----------------------------------------------------------------------------------------------
// <copyright file="TestController.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Getter.Api.Controllers.Base;

namespace Gpn.Template.Getter.Api.Controllers;

/// <summary>
/// Контроллер инвестиционного плана
/// </summary>
/// <param name="logger">Логгер.</param>
public sealed class TestController(ILogger<TestController> logger) : GetterControllerBase(logger)
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
