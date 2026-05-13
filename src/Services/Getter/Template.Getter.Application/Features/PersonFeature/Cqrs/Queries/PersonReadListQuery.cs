// ----------------------------------------------------------------------------------------------
// <copyright file="PersonReadListQuery.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;
using Template.Domain.Entities;
using Template.Getter.Application.Abstractions.Dto.Person.Filters;
using Template.Getter.Application.Abstractions.Dto.Person.Requests;
using Template.Getter.Application.Abstractions.Dto.Person.Responses;

namespace Template.Getter.Application.Features.PersonFeature.Cqrs.Queries;

/// <summary>
/// Чтения с пагинацией, фильтрами и сортировкой сущностей <see cref="Person"/>.
/// </summary>
public class PersonReadListQuery(PersonListRequest request)
    : ReadListQuery<PersonListRequest, PersonListFilter, PersonListResponse>(request);
