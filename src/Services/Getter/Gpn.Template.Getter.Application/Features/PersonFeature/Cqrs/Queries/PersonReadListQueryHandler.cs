// ----------------------------------------------------------------------------------------------
// <copyright file="PersonReadListQueryHandler.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Filters;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Requests;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Responses;
using Gpn.Template.Getter.Application.Specifications;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.Dal.Repository.Models;
using Shared.Application.Core.Dal.UnitOfWork.Interfaces;
using Shared.Application.Cqrs.Core.Abstractions.Queries.Handlers;
using Shared.Application.Cqrs.Core.Utils.PostProcessors;

namespace Gpn.Template.Getter.Application.Features.PersonFeature.Cqrs.Queries;

/// <summary>
/// Handler множественного чтения для <see cref="Person"/>.
/// </summary>
public class PersonReadListQueryHandler(
    ILoggerFactory loggerFactory,
    IUnitOfWork unitOfWork,
    IRepository<Person>? repository = default,
    IDtoPostProcessor<PersonListPayload>? postProcessor = default)
    : ReadListQueryHandler<PersonReadListQuery, PersonListRequest, PersonListResponse, Person, PersonListPayload, PersonListFilter>(
        loggerFactory,
        repository ?? unitOfWork.GetRepository<Person>(),
        postProcessor,
        (totalPages, items, status) => new PersonListResponse(totalPages, items, status))
{
    /// <inheritdoc />
    protected override QueryOptions<Person> ConstructOptions(PersonReadListQuery request)
    {
        var specification = new PersonSpecification();
        var options = specification.BuildOptions();
        ApplySortOptions(request.SortOptions, options);
        return options;
    }
}
