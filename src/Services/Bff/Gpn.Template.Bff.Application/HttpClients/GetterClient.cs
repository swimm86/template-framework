// ----------------------------------------------------------------------------------------------
// <copyright file="GetterClient.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Application.Interfaces.HttpClients;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Requests;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.ApiClient;
using Shared.Application.Core.ApiClient.Interfaces;
using Shared.Domain.Core.Utils.Interfaces;

namespace Gpn.Template.Bff.Application.HttpClients;

/// <summary>
/// Http клиент Getter.
/// </summary>
public sealed class GetterClient(
    IHttpClientFactory httpClientFactory,
    IUriValidator uriValidator,
    IResponseValidator responseValidator,
    IPropertyGetter propertyGetter,
    IHttpContextAccessor httpContextAccessor,
    ILogger<GetterClient> logger)
    : ApiClient(
        httpClientFactory,
        uriValidator,
        responseValidator,
        propertyGetter,
        httpContextAccessor,
        logger), IGetterClient
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
