// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListQuery.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;
using Template.Getter.Application.Abstractions.Features.Person.List.Request;
using Template.Getter.Application.Abstractions.Features.Person.List.Response;

namespace Template.Getter.Application.Features.Person.Cqrs.List;

/// <summary>
/// Запрос чтения с пагинацией, фильтрами и сортировкой сущностей "Персона".
/// </summary>
public class PersonListQuery(PersonListRequest request)
    : ReadListQuery<PersonListRequest, PersonListFilter, PersonListResponse>(request);
