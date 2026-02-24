// ----------------------------------------------------------------------------------------------
// <copyright file="GetterClient.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Application.Interfaces.HttpClients;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Requests;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Responses;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.ApiClient;

namespace Gpn.Template.Bff.Application.HttpClients;

/// <summary>
/// Http клиент Getter.
/// </summary>
/// <param name="httpClientFactory">Фабрика HTTP-клиентов.</param>
public sealed class GetterClient(
    IHttpClientFactory httpClientFactory,
    ILogger<GetterClient> logger)
    : ApiClient(httpClientFactory, logger), IGetterClient
{
    /// <inheritdoc />
    public Task<PersonListResponse> GetPersonsAsync(
        PersonListRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<PersonListResponse>(
            "persons/list",
            request,
            cancellationToken)!;
    }

    /// <inheritdoc />
    public Task<PersonListResponse> GetPersonsCqrsAsync(
        PersonListRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<PersonListResponse>(
            "persons-cqrs/list",
            request,
            cancellationToken)!;
    }
}
