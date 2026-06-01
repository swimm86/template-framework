// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListQuery.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Cqrs.Core.Abstractions.Queries;
using Template.Bff.Application.Features.Queries.Person.Cqrs.List.Requests;
using Template.Getter.Application.Abstractions.Features.Person.List.Response;

namespace Template.Bff.Application.Features.Queries.Person.Cqrs.List;

/// <summary>
/// Запрос чтения с пагинацией, фильтрами и сортировкой сущностей "Персона".
/// </summary>
/// <param name="Request">Параметры запроса списка персон с фильтрацией, сортировкой и пагинацией.</param>
public record PersonListQuery(PersonListRequest Request)
    : IQuery<PersonListResponse>;
