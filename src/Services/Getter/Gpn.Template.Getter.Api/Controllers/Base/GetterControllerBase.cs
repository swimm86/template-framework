// ----------------------------------------------------------------------------------------------
// <copyright file="GetterControllerBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Presentation;
using Shared.Presentation.Core.Attributes;
using ControllerBase = Shared.Presentation.Core.Controllers.ControllerBase;

namespace Gpn.Template.Getter.Api.Controllers.Base;

/// <summary>
/// Базовый класс для Getter Controller-ов
/// </summary>
/// <param name="logger">Логгер.</param>
[AppName(Constants.AppName)]
[ControllerType("getter")]
public abstract class GetterControllerBase(
    ILogger logger)
    : ControllerBase(logger);
