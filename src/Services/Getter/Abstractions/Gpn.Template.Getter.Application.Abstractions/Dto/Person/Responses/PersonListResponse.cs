// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListResponse.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Responses;

namespace Gpn.Template.Getter.Application.Abstractions.Dto.Person.Responses;

/// <summary>
/// Ответ на запрос с информацией об 'Person-ах'.
/// </summary>
/// <param name="TotalPages">Количество страниц.</param>
/// <param name="Payload">Информация об 'Person-ах'.</param>
/// <param name="StatusCode">Статус ответа.</param>
public record PersonListResponse(int TotalPages, ICollection<PersonListPayload> Payload, int StatusCode)
    : PageableResponse<ICollection<PersonListPayload>>(TotalPages, Payload, StatusCode);

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
