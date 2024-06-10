// ----------------------------------------------------------------------------------------------
// <copyright file="ControllerBase.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Shared.Presentation.Core.Controllers;

/// <summary>
/// Базовый класс для контроллеров
/// </summary>
[ApiController]
[Route("api/pps/[controllerType]/v1/[controller]")]
public abstract class ControllerBase(ILogger logger) : Microsoft.AspNetCore.Mvc.ControllerBase
{
}
