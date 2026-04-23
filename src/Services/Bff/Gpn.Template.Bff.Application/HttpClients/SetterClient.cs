// ----------------------------------------------------------------------------------------------
// <copyright file="SetterClient.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Application.Interfaces.HttpClients;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
    IPropertyGetter propertyGetter,
    IHttpContextAccessor httpContextAccessor,
    ILogger<SetterClient> logger)
    : ApiClient(
        httpClientFactory,
        uriValidator,
        responseValidator,
        propertyGetter,
        httpContextAccessor,
        logger), ISetterClient;
