// ----------------------------------------------------------------------------------------------
// <copyright file="SetterControllerBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Presentation.Core.Attributes;
using Template.Presentation;

using ControllerBase = Shared.Presentation.Core.Controllers.ControllerBase;

namespace Template.Setter.Api.Controllers.Base;

/// <summary>
/// Базовый класс для Setter Controller-ов.
/// </summary>
/// <param name="logger">Экземпляр <see cref="ILogger"/> для работы с логированием.</param>
[AppName(Constants.AppName)]
[ControllerType("setter")]
public abstract class SetterControllerBase(
    ILogger logger)
    : ControllerBase(logger);
