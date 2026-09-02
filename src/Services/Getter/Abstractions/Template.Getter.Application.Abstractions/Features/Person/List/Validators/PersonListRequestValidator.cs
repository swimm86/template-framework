// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListRequestValidator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation;
using Shared.Application.Core.Dto.Requests.Validators;
using Template.Getter.Application.Abstractions.Features.Person.List.Request;

namespace Template.Getter.Application.Abstractions.Features.Person.List.Validators;

/// <summary>
/// Валидатор запроса '<see cref="PersonListRequest"/>'.
/// </summary>
public class PersonListRequestValidator
    : PageableRequestValidator<PersonListRequest>
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

        RuleFor(x => x.PageSize)
            .LessThanOrEqualTo(1000)
            .WithMessage("Размер страницы не должен превышать 1000.");

        RuleFor(x => x.Filter!)
            .SetValidator(personListFilterValidator)
            .When(x => x.Filter != null);
    }
}
