// ----------------------------------------------------------------------------------------------
// <copyright file="PersonCreateDto.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Template.Application.Dto.Person;

/// <summary>
/// Dto для создания сущности "Персона".
/// </summary>
public record PersonCreateDto
{
    /// <inheritdoc cref="Domain.Entities.Person.Name" path="/summary"/>
    public string Name { get; init; }

    /// <inheritdoc cref="Domain.Entities.Person.Email" path="/summary"/>
    public string Email { get; init; }
}