// ----------------------------------------------------------------------------------------------
// <copyright file="SetterControllerBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Presentation;
using Shared.Presentation.Core.Attributes;
using ControllerBase = Shared.Presentation.Core.Controllers.ControllerBase;

namespace Gpn.Template.Setter.Api.Controllers.Base;

/// <summary>
/// Базовый класс для Setter Controller-ов.
/// </summary>
/// <param name="logger">Логгер.</param>
[AppName(Constants.AppName)]
[ControllerType("setter")]
public abstract class SetterControllerBase(
    ILogger logger)
    : ControllerBase(logger);
