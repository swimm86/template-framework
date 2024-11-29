// ----------------------------------------------------------------------------------------------
// <copyright file="SetterControllerBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Presentation.Core.Attributes;
using ControllerBase = Shared.Presentation.Core.Controllers.ControllerBase;

namespace Gpn.Template.Setter.Api.Controllers.Base;

/// <summary>
/// Базовый класс для Getter Controller-ов
/// </summary>
/// <param name="logger">Логгер.</param>
[ControllerType("setter")]
public abstract class SetterControllerBase(ILogger logger) : ControllerBase(logger)
{
}
