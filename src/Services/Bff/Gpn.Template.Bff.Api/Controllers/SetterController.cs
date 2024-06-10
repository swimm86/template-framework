// ----------------------------------------------------------------------------------------------
// <copyright file="SetterController.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Api.Controllers.Base;
using Gpn.Template.Bff.Api.Interfaces;
using Gpn.Template.Bff.Application.Interfaces.HttpClients;

namespace Gpn.Template.Bff.Api.Controllers;

/// <summary>
/// Setter контроллер
/// </summary>
/// <param name="setterClient">Клиент Pps.Setter.</param>
/// <param name="logger">Логгер.</param>
public class SetterController(
    ISetterClient setterClient,
    ILogger<IGetterController> logger
) : BffControllerBase(logger), ISetterController
{
}
