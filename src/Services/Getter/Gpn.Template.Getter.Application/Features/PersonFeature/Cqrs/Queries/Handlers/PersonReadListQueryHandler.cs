// ----------------------------------------------------------------------------------------------
// <copyright file="PersonReadListQueryHandler.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Gpn.Template.Getter.Application.Features.PersonFeature.Cqrs.Queries.Filters;
using Gpn.Template.Getter.Application.Features.PersonFeature.Dtos.Requests;
using Gpn.Template.Getter.Application.Features.PersonFeature.Dtos.Responses;
using Gpn.Template.Getter.Application.Specifications;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.Dal.Repository.Models;
using Shared.Application.Core.Dal.UnitOfWork.Interfaces;
using Shared.Application.Cqrs.Core.Abstractions.Queries.Handlers;
using Shared.Application.Cqrs.Core.Utils.PostProcessors;

namespace Gpn.Template.Getter.Application.Features.PersonFeature.Cqrs.Queries.Handlers;

/// <summary>
/// Handler множественного чтения для <see cref="Person"/>.
/// </summary>
public class PersonReadListQueryHandler(
    ILoggerFactory loggerFactory,
    IUnitOfWork unitOfWork,
    IRepository<Person>? repository = default,
    IDtoPostProcessor<PersonDto>? postProcessor = default)
    : ReadListQueryHandler<PersonReadListQuery, GetPersonsRequestDto, Person, PersonDto, PersonFilter>(
        loggerFactory,
        repository ?? unitOfWork.GetRepository<Person>(),
        postProcessor)
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
