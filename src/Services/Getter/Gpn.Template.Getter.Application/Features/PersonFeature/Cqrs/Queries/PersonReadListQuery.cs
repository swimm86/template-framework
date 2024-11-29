// ----------------------------------------------------------------------------------------------
// <copyright file="PersonReadListQuery.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Filters;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Requests;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Responses;
using Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;

namespace Gpn.Template.Getter.Application.Features.PersonFeature.Cqrs.Queries;

/// <summary>
/// Чтения с пагинацией, фильтрами и сортировкой сущностей <see cref="Person"/>.
/// </summary>
public class PersonReadListQuery(PersonListRequest request)
    : ReadListQuery<PersonListRequest, PersonListFilter, PersonListResponse>(request)
{
}
