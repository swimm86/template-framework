// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListResponse.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Responses;
using Template.Application.Dto.Person;

namespace Template.Getter.Application.Abstractions.Features.Person.List.Response;

/// <summary>
/// Ответ на запрос с информацией о сущностях "Персона".
/// </summary>
public record PersonListResponse
    : PageableResponse<ICollection<PersonListPayload>>;

/// <summary>
/// DTO с информацией о конкретной сущности "Персона".
/// </summary>
/// <remarks>
/// Наследует <see cref="PersonDto"/>. Зарезервировано для добавления полей, специфичных для контракта.
/// </remarks>
public sealed record PersonListPayload
    : PersonDto;
