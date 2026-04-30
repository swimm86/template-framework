// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListQueryHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Application.HttpClients.Enums;
using Gpn.Template.Bff.Application.Interfaces.HttpClients;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Responses;
using Shared.Application.Cqrs.Core.Abstractions.Queries.Handlers;

namespace Gpn.Template.Bff.Application.Features.Person.Cqrs.Queries.List;

/// <summary>
/// Handler для <see cref="PersonListQuery"/>.
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
