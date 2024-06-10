// ----------------------------------------------------------------------------------------------
// <copyright file="GetterService.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Application.Interfaces.HttpClients;
using Gpn.Template.Bff.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Gpn.Template.Bff.Application.Services;

/// <summary>
/// Сервис Setter
/// </summary>
internal sealed class GetterService(
    IGetterClient getterClient,
    ILogger<GetterService> logger
) : IGetterService
{
}
