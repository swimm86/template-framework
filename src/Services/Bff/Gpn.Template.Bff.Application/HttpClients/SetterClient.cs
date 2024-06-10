// ----------------------------------------------------------------------------------------------
// <copyright file="SetterClient.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Application.Interfaces.HttpClients;
using Shared.Application.Core.ApiClient;

namespace Gpn.Template.Bff.Application.HttpClients;

/// <summary>
/// Клиент Pps.Export.Setter
/// </summary>
/// <param name="clientFactory">Фабрика HTTP-клиентов.</param>
public sealed class SetterClient(
    IHttpClientFactory clientFactory
) : ApiClient(clientFactory), ISetterClient
{
}
