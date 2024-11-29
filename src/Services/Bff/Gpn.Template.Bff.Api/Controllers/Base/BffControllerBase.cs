// ----------------------------------------------------------------------------------------------
// <copyright file="BffControllerBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Presentation.Core.Attributes;
using ControllerBase = Shared.Presentation.Core.Controllers.ControllerBase;

namespace Gpn.Template.Bff.Api.Controllers.Base;

/// <summary>
/// Базовый класс для Getter Controller-ов
/// </summary>
/// <param name="logger">Логгер.</param>
[ControllerType("bff")]
public abstract class BffControllerBase(ILogger logger) : ControllerBase(logger)
{
}
