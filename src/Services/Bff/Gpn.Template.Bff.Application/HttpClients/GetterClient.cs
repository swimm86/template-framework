// ----------------------------------------------------------------------------------------------
// <copyright file="GetterClient.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Application.Interfaces.HttpClients;
using Shared.Application.Core.ApiClient;

namespace Gpn.Template.Bff.Application.HttpClients;

/// <summary>
/// Http клиент Getter
/// </summary>
/// <param name="httpClientFactory"><see cref="IHttpClientFactory"/>.</param>
public sealed class GetterClient(
    IHttpClientFactory httpClientFactory
) : ApiClient(httpClientFactory), IGetterClient
{
}
