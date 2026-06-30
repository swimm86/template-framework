// ----------------------------------------------------------------------------------------------
// <copyright file="GetterClient.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.ApiClient;
using Shared.Application.Core.ApiClient.Attributes;
using Shared.Application.Core.ApiClient.Validators.Interfaces;
using Shared.Application.Core.Dto.Responses;
using Shared.Domain.Core.Utils.Interfaces;
using Template.Setter.Application.HttpClients.Settings;
using Template.Setter.Application.Interfaces.HttpClients;

namespace Template.Setter.Application.HttpClients;

/// <summary>
/// API-клиент Getter-а.
/// </summary>
[ApiClientRegistration(typeof(GetterApiClientSettings), typeof(IGetterClient))]
public sealed class GetterClient(
    IHttpClientFactory httpClientFactory,
    IUriValidator uriValidator,
    IResponseValidator responseValidator,
    IPropertyGetter propertyGetter)
    : ApiClient(
        httpClientFactory,
        uriValidator,
        responseValidator,
        propertyGetter), IGetterClient
{
/// <inheritdoc />
    public Task<Response> TestExceptionChainAsync(
    CancellationToken cancellationToken = default)
    {
        return PostAsync<Response>("test/exception-chain", cancellationToken)!;
    }
}
