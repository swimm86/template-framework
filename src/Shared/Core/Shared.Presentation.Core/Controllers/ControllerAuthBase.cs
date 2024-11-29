// ----------------------------------------------------------------------------------------------
// <copyright file="ControllerAuthBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Shared.Presentation.Core.Controllers;

/// <summary>
/// Контроллер с фильтром аутентификации
/// </summary>
[ApiController]
[Authorize]
public abstract class ControllerAuthBase(ILogger logger) : ControllerBase(logger)
{
}