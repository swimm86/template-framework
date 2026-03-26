// ----------------------------------------------------------------------------------------------
// <copyright file="GetterControllerBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
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
