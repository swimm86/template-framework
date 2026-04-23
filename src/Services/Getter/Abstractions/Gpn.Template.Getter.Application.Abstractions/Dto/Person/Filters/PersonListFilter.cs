// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListFilter.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Gpn.Template.Getter.Application.Abstractions.Dto.Person.Filters;

/// <summary>
/// Фильтра для <see cref="Person"/>.
/// </summary>
public class PersonListFilter
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
