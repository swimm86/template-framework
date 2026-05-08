// ----------------------------------------------------------------------------------------------
// <copyright file="BffControllerBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Presentation.Core.Attributes;
using Template.Presentation;

using ControllerBase = Shared.Presentation.Core.Controllers.ControllerBase;

namespace Template.Bff.Api.Controllers.Base;

/// <summary>
/// Базовый класс для BFF Controller-ов.
/// </summary>
/// <param name="logger">Логгер.</param>
[AppName(Constants.AppName)]
[ControllerType("bff")]
public abstract class BffControllerBase(
    ILogger logger)
    : ControllerBase(logger);
