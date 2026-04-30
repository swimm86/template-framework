// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListQuery.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Application.Features.Person.Cqrs.Queries.List.Requests;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Responses;
using Shared.Application.Cqrs.Core.Abstractions.Queries;

namespace Gpn.Template.Bff.Application.Features.Person.Cqrs.Queries.List;

/// <summary>
/// Чтения с пагинацией, фильтрами и сортировкой сущностей <see cref="Person"/>.
/// </summary>
/// <param name="Request">Параметры запроса списка персон с фильтрацией, сортировкой и пагинацией.</param>
public record PersonListQuery(PersonListRequest Request)
    : IQuery<PersonListResponse>;
