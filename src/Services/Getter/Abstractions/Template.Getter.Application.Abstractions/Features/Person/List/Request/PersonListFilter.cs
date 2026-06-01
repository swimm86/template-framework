// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListFilter.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Template.Getter.Application.Abstractions.Features.Person.List.Request;

/// <summary>
/// Фильтр для сущности "Персона".
/// </summary>
public record PersonListFilter
{
    /// <summary>
    /// <inheritdoc cref="Domain.Entities.Person.Name" path="/summary"/>
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Часть от имени.
    /// </summary>
    public string? NameContains { get; init; }

    /// <summary>
    /// <inheritdoc cref="Domain.Entities.Person.Email" path="/summary"/>
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Часть от адреса электронной почты.
    /// </summary>
    public string? EmailContains { get; init; }
}
