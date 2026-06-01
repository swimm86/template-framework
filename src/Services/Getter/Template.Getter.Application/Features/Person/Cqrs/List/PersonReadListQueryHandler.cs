// ----------------------------------------------------------------------------------------------
// <copyright file="PersonReadListQueryHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Shared.Application.Cqrs.Core.Abstractions.Queries.Handlers;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Template.Getter.Application.Abstractions.Features.Person.List.Request;
using Template.Getter.Application.Abstractions.Features.Person.List.Response;
using Template.Getter.Application.Specifications;

namespace Template.Getter.Application.Features.Person.Cqrs.List;

/// <summary>
/// Обработчик множественного чтения для "Персона".
/// </summary>
public class PersonReadListQueryHandler(
    ILoggerFactory loggerFactory,
    IUnitOfWork unitOfWork)
    : ReadListQueryHandler<
        PersonListQuery,
        PersonListRequest,
        PersonListFilter,
        PersonListResponse,
        PersonListPayload,
        Domain.Entities.Person>(
        loggerFactory,
        unitOfWork)
{
    /// <inheritdoc />
    protected override QueryOptions<Domain.Entities.Person> ConstructOptions(
        PersonListQuery query)
    {
        var specification = new PersonSpecification(query.Request);
        var options = specification.BuildOptions();
        ApplySortOptions(query.Request.ConvertSortOptions(), options);
        return options;
    }
}
