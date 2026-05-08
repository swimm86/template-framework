// ----------------------------------------------------------------------------------------------
// <copyright file="PersonReadListQueryHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Shared.Application.Cqrs.Core.Abstractions.Queries.Handlers;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Template.Domain.Entities;
using Template.Getter.Application.Abstractions.Dto.Person.Filters;
using Template.Getter.Application.Abstractions.Dto.Person.Requests;
using Template.Getter.Application.Abstractions.Dto.Person.Responses;
using Template.Getter.Application.Specifications;

namespace Template.Getter.Application.Features.PersonFeature.Cqrs.Queries;

/// <summary>
/// Handler множественного чтения для <see cref="Person"/>.
/// </summary>
public class PersonReadListQueryHandler(
    ILoggerFactory loggerFactory,
    IUnitOfWork unitOfWork)
    : ReadListQueryHandler<PersonReadListQuery, PersonListRequest, PersonListFilter, PersonListResponse, PersonListPayload, Person>(
        loggerFactory,
        unitOfWork)
{
    /// <inheritdoc />
    protected override QueryOptions<Person> ConstructOptions(PersonReadListQuery query)
    {
        var specification = new PersonSpecification(query.Request);
        var options = specification.BuildOptions();
        ApplySortOptions(query.Request.ConvertSortOptions(), options);
        return options;
    }
}
