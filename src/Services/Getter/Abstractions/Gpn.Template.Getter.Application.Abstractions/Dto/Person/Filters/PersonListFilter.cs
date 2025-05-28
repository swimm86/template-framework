// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListFilter.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Requests;

namespace Gpn.Template.Getter.Application.Abstractions.Dto.Person.Filters;

/// <summary>
/// Фильтра для <see cref="Domain.Entities.Person"/>.
/// </summary>
public record PersonListFilter
    : FilterBase
{
    /// <summary>
    /// <inheritdoc cref="Domain.Entities.Person.Name"/>
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// <inheritdoc cref="Domain.Entities.Person.Email"/>
    /// </summary>
    public string? Email { get; set; }
}
