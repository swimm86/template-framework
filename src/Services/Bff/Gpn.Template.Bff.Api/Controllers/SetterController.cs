// ----------------------------------------------------------------------------------------------
// <copyright file="SetterController.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Api.Controllers.Base;
using Gpn.Template.Bff.Api.Interfaces;
using Gpn.Template.Bff.Application.Interfaces.HttpClients;

namespace Gpn.Template.Bff.Api.Controllers;

/// <summary>
/// Setter контроллер
/// </summary>
public class SetterController(
    ISetterClient setterClient,
    ILogger<SetterController> logger)
    : BffControllerBase(logger), ISetterController;
