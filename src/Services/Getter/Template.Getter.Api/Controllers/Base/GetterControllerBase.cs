// ----------------------------------------------------------------------------------------------
// <copyright file="GetterControllerBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Presentation.Core.Attributes;
using Template.Presentation;

using ControllerBase = Shared.Presentation.Core.Controllers.ControllerBase;

namespace Template.Getter.Api.Controllers.Base;

/// <summary>
/// Базовый класс для Getter Controller-ов.
/// </summary>
/// <param name="logger">Экземпляр <see cref="ILogger"/> для работы с логированием.</param>
[AppName(Constants.AppName)]
[ControllerType("getter")]
public abstract class GetterControllerBase(
    ILogger logger)
    : ControllerBase(logger);
