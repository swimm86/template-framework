// ----------------------------------------------------------------------------------------------
// <copyright file="PersonCreateResponse.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Cqrs.Core.Abstractions.Commands.Responses;
using Template.Application.Dto.Person;

namespace Template.Setter.Application.Abstractions.Features.Person.Create.Response;

/// <summary>
/// Ответ на запрос создания сущности "Персона".
/// </summary>
public sealed record PersonCreateResponse
    : CreateResponse<PersonDto>;
