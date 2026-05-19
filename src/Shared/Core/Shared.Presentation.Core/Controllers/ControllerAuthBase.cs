// ----------------------------------------------------------------------------------------------
// <copyright file="ControllerAuthBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Shared.Presentation.Core.Controllers;

/// <summary>
/// Базовый класс контроллера с обязательной аутентификацией.
/// </summary>
[ApiController]
[Authorize]
public abstract class ControllerAuthBase(ILogger logger)
    : ControllerBase(logger);