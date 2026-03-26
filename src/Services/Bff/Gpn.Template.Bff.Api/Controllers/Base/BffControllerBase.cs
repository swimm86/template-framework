// ----------------------------------------------------------------------------------------------
// <copyright file="BffControllerBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Presentation;
using Shared.Presentation.Core.Attributes;
using ControllerBase = Shared.Presentation.Core.Controllers.ControllerBase;

namespace Gpn.Template.Bff.Api.Controllers.Base;

/// <summary>
/// Базовый класс для BFF Controller-ов.
/// </summary>
/// <param name="logger">Логгер.</param>
[AppName(Constants.AppName)]
[ControllerType("bff")]
public abstract class BffControllerBase(
    ILogger logger)
    : ControllerBase(logger);
