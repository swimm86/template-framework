// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListRequestValidator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation;
using Template.Getter.Application.Abstractions.Features.Person.List.Request;

namespace Template.Getter.Application.Abstractions.Features.Person.List.Validators;

/// <summary>
/// Валидатор запроса '<see cref="PersonListRequest"/>'.
/// </summary>
public class PersonListRequestValidator
    : AbstractValidator<PersonListRequest>
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="PersonListRequestValidator"/>.
    /// </summary>
    /// <param name="personListFilterValidator">
    /// <inheritdoc cref="PersonListFilterValidator" path="/summary"/>
    /// </param>
    public PersonListRequestValidator(
        PersonListFilterValidator personListFilterValidator)
    {
        RuleFor(x => x.DalPattern)
            .IsInEnum()
            .WithMessage("Недопустимый паттерн доступа к данным.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Номер страницы должен быть не менее 1.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Размер страницы должен быть больше 0.")
            .LessThanOrEqualTo(1000)
            .WithMessage("Размер страницы не должен превышать 1000.");

        RuleFor(x => x.Filter!)
            .SetValidator(personListFilterValidator)
            .When(x => x.Filter != null);
    }
}