// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListResponse.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Responses;

namespace Template.Getter.Application.Abstractions.Dto.Person.Responses;

/// <summary>
/// Ответ на запрос с информацией об 'Person-ах'.
/// </summary>
public record PersonListResponse
    : PageableResponse<ICollection<PersonListPayload>>;

/// <summary>
/// Payload-Dto с информацией об конкретном 'Person-е'.
/// </summary>
public sealed record PersonListPayload
{
    /// <summary>
    /// Идентификатор.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Имя.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Email.
    /// </summary>
    public string Email { get; init; }
}
