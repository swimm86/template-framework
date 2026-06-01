// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListQueryValidator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation;
using Template.Getter.Application.Abstractions.Features.Person.List.Validators;

namespace Template.Bff.Application.Features.Queries.Person.Cqrs.List.Validators;

/// <summary>
/// Валидатор запроса '<see cref="PersonListQuery"/>'.
/// </summary>
public class PersonListQueryValidator
    : AbstractValidator<PersonListQuery>
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="PersonListQueryValidator"/>.
    /// </summary>
    /// <param name="personListRequestValidator">
    /// <inheritdoc cref="PersonListRequestValidator" path="/summary"/>
    /// </param>
    public PersonListQueryValidator(
        PersonListRequestValidator personListRequestValidator)
    {
        RuleFor(x => x.Request)
            .SetValidator(personListRequestValidator);
    }
}