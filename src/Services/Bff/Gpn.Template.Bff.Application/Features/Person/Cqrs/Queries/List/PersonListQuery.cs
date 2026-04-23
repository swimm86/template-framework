// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListQuery.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Application.Dto.Requests;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Responses;
using Shared.Application.Cqrs.Core.Abstractions.Queries;

namespace Gpn.Template.Bff.Application.Features.Person.Cqrs.Queries.List;

/// <summary>
/// Чтения с пагинацией, фильтрами и сортировкой сущностей <see cref="Person"/>.
/// </summary>
/// <param name="Request"></param>
public record PersonListQuery(PersonListRequest Request) : IQuery<PersonListResponse>;
