// ----------------------------------------------------------------------------------------------
// <copyright file="SetterController.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Api.Controllers.Base;
using Gpn.Template.Bff.Api.Interfaces;
using Gpn.Template.Bff.Application.Interfaces.HttpClients;

namespace Gpn.Template.Bff.Api.Controllers;

/// <summary>
/// Setter контроллер
/// </summary>
/// <param name="setterClient">Клиент Setter.</param>
/// <param name="logger">Логгер.</param>
public class SetterController(
    ISetterClient setterClient,
    ILogger<SetterController> logger)
    : BffControllerBase(logger), ISetterController
{
}
