// ----------------------------------------------------------------------------------------------
// <copyright file="PersonFilter.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;

namespace Gpn.Template.Getter.Application.Features.PersonFeature.Cqrs.Queries.Filters;

/// <summary>
/// Фильтра для <see cref="Person"/>.
/// </summary>
public class PersonFilter
{
    /// <summary>
    /// <inheritdoc cref="Person.Name"/>
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Часть от имени.
    /// </summary>
    public string? NameContains { get; set; }

    /// <summary>
    /// <inheritdoc cref="Person.Email"/>
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Часть от адреса электронной почты.
    /// </summary>
    public string? EmailContains { get; set; }
}
