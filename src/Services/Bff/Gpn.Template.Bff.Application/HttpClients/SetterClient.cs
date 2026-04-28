// ----------------------------------------------------------------------------------------------
// <copyright file="SetterClient.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Application.HttpClients.Settings;
using Gpn.Template.Bff.Application.Interfaces.HttpClients;
using Shared.Application.Core.ApiClient;
using Shared.Application.Core.ApiClient.Attributes;
using Shared.Application.Core.ApiClient.Validators.Interfaces;
using Shared.Domain.Core.Utils.Interfaces;

namespace Gpn.Template.Bff.Application.HttpClients;

/// <summary>
/// API-клиент Setter-а.
/// </summary>
[ApiClientRegistration(typeof(SetterApiClientSettings), typeof(ISetterClient))]
public sealed class SetterClient(
    IHttpClientFactory httpClientFactory,
    IUriValidator uriValidator,
    IResponseValidator responseValidator,
    IPropertyGetter propertyGetter)
    : ApiClient(
        httpClientFactory,
        uriValidator,
        responseValidator,
        propertyGetter), ISetterClient;
