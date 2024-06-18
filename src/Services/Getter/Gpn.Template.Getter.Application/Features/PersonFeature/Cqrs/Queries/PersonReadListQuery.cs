// ----------------------------------------------------------------------------------------------
// <copyright file="PersonReadListQuery.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Gpn.Template.Getter.Application.Features.PersonFeature.Cqrs.Queries.Filters;
using Gpn.Template.Getter.Application.Features.PersonFeature.Dtos.Requests;
using Gpn.Template.Getter.Application.Features.PersonFeature.Dtos.Responses;
using Shared.Application.Core.Dto.Responses;
using Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;

namespace Gpn.Template.Getter.Application.Features.PersonFeature.Cqrs.Queries;

/// <summary>
/// Чтения с пагинацией, фильтрами и сортировкой сущностей <see cref="Person"/>.
/// </summary>
public class PersonReadListQuery(GetPersonsRequestDto request)
    : ReadListQuery<GetPersonsRequestDto, PersonFilter, PageableResponse<ICollection<PersonDto>>>(request)
{
}
