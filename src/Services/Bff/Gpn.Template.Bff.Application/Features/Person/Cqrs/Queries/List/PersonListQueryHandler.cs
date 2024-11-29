// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListQueryHandler.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

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
        return request.Request.UseCqrs
            ? getterClient.GetPersonsCqrsAsync(request.Request, cancellationToken)
            : getterClient.GetPersonsAsync(request.Request, cancellationToken);
    }
}
