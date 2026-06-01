// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListQueryHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Cqrs.Core.Abstractions.Queries.Handlers;
using Template.Bff.Application.HttpClients.Enums;
using Template.Bff.Application.Interfaces.HttpClients;
using Template.Getter.Application.Abstractions.Features.Person.List.Response;

namespace Template.Bff.Application.Features.Queries.Person.Cqrs.List;

/// <summary>
/// Обработчик <see cref="PersonListQuery"/>.
/// </summary>
/// <param name="getterClient">API-клиент для Getter-а.</param>
public class PersonListQueryHandler(
    IGetterClient getterClient)
    : IQueryHandler<PersonListQuery, PersonListResponse>
{
    /// <inheritdoc />
    public Task<PersonListResponse> Handle(
        PersonListQuery request,
        CancellationToken cancellationToken)
    {
        return getterClient.GetPersonsAsync(
            request.Request,
            request.Request.UseCqrs ? GetPersonsPattern.Cqrs : GetPersonsPattern.Services,
            cancellationToken);
    }
}
