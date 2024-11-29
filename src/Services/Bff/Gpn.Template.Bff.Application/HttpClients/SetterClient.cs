// ----------------------------------------------------------------------------------------------
// <copyright file="SetterClient.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Application.Interfaces.HttpClients;
using Shared.Application.Core.ApiClient;

namespace Gpn.Template.Bff.Application.HttpClients;

/// <summary>
/// Http клиент Setter.
/// </summary>
/// <param name="clientFactory">Фабрика HTTP-клиентов.</param>
public sealed class SetterClient(
    IHttpClientFactory clientFactory)
    : ApiClient(clientFactory), ISetterClient
{
}
