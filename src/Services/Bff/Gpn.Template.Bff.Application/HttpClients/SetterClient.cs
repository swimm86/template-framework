// ----------------------------------------------------------------------------------------------
// <copyright file="SetterClient.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Application.Interfaces.HttpClients;
using Shared.Application.Core.ApiClient;
using Shared.Application.Core.ApiClient.Interfaces;
using Shared.Domain.Core.Utils.Interfaces;

namespace Gpn.Template.Bff.Application.HttpClients;

/// <summary>
/// Http клиент Setter.
/// </summary>
public sealed class SetterClient(
    IHttpClientFactory httpClientFactory,
    IUriValidator uriValidator,
    IResponseValidator responseValidator,
    IPropertyGetter propertyGetter)
    : ApiClient(httpClientFactory, uriValidator, responseValidator, propertyGetter), ISetterClient;
