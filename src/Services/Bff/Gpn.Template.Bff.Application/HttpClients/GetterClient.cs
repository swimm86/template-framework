// ----------------------------------------------------------------------------------------------
// <copyright file="GetterClient.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Application.Interfaces.HttpClients;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Requests;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Responses;
using Shared.Application.Core.ApiClient;

namespace Gpn.Template.Bff.Application.HttpClients;

/// <summary>
/// Http клиент Getter.
/// </summary>
/// <param name="httpClientFactory">Фабрика HTTP-клиентов.</param>
public sealed class GetterClient(
    IHttpClientFactory httpClientFactory)
    : ApiClient(httpClientFactory), IGetterClient
{
    /// <inheritdoc />
    public Task<PersonListResponse> GetPersonsAsync(
        PersonListRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<PersonListRequest, PersonListResponse>(
            "persons/list",
            request,
            cancellationToken)!;
    }

    /// <inheritdoc />
    public Task<PersonListResponse> GetPersonsCqrsAsync(
        PersonListRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<PersonListRequest, PersonListResponse>(
            "persons-cqrs/list",
            request,
            cancellationToken)!;
    }
}
