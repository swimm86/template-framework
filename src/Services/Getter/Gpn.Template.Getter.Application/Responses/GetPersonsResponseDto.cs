// ----------------------------------------------------------------------------------------------
// <copyright file="GetPersonsResponseDto.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;

namespace Gpn.Template.Getter.Application.Responses;

/// <summary>
/// Response-Dto для получения всех 'Person-ов'.
/// </summary>
public sealed record GetPersonsResponseDto()
{
    /// <summary>
    /// Список 'Person-ов'.
    /// </summary>
    public ICollection<PersonDto> Persons { get; init; } = [];
}

/// <summary>
/// Response-Dto с информацией об конкретном 'Person-е'.
/// </summary>
public sealed record PersonDto
{
    /// <summary>
    /// <inheritdoc cref="Person.Id"/>
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// <inheritdoc cref="Person.Name"/>
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// <inheritdoc cref="Person.Email"/>
    /// </summary>
    public string Email { get; init; }
}
