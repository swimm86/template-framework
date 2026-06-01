// ----------------------------------------------------------------------------------------------
// <copyright file="GetterClient.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.ApiClient;
using Shared.Application.Core.ApiClient.Attributes;
using Shared.Application.Core.ApiClient.Validators.Interfaces;
using Shared.Domain.Core.Utils.Interfaces;
using Template.Bff.Application.HttpClients.Enums;
using Template.Bff.Application.HttpClients.Settings;
using Template.Bff.Application.Interfaces.HttpClients;
using Template.Getter.Application.Abstractions.Features.Person.List.Request;
using Template.Getter.Application.Abstractions.Features.Person.List.Response;

namespace Template.Bff.Application.HttpClients;

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
    public Task<PersonListResponse> GetPersonsAsync(
        PersonListRequest request,
        GetPersonsPattern pattern,
        CancellationToken cancellationToken = default)
    {
        var requestPart = pattern switch
        {
            GetPersonsPattern.Cqrs => "cqrs",
            GetPersonsPattern.Services => "services",
            _ => throw new ArgumentException("Неизвестное значение GetPersonsPattern.", nameof(pattern))
        };

        return PostAsync<PersonListResponse>(
            $"persons/{requestPart}/list",
            request,
            cancellationToken)!;
    }
}
