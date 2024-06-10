// ----------------------------------------------------------------------------------------------
// <copyright file="GetterController.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Api.Controllers.Base;
using Gpn.Template.Bff.Api.Interfaces;
using Gpn.Template.Bff.Application.Interfaces.HttpClients;

namespace Gpn.Template.Bff.Api.Controllers;

/// <summary>
/// Контроллер Getter
/// </summary>
/// <param name="logger">Логгер.</param>
/// <param name="getterClient">Клиент геттера.</param>
public class GetterController(
    IGetterClient getterClient,
    ILogger<IGetterController> logger
) : BffControllerBase(logger), IGetterController
{
}
