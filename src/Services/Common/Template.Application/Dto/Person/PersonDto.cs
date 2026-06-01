// ----------------------------------------------------------------------------------------------
// <copyright file="PersonDto.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Template.Application.Dto.Person;

/// <summary>
/// DTO для сущности "Персона".
/// </summary>
public record PersonDto
    : IEntity<Guid>
{
    /// <inheritdoc />
    public Guid Id { get; init; }

    /// <inheritdoc cref="Domain.Entities.Person.Name" path="/summary"/>
    public string Name { get; init; }

    /// <inheritdoc cref="Domain.Entities.Person.Email" path="/summary"/>
    public string Email { get; init; }
}
