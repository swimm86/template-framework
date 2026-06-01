// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListFilterValidator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using FluentValidation;
using Template.Getter.Application.Abstractions.Features.Person.List.Request;

namespace Template.Getter.Application.Abstractions.Features.Person.List.Validators;

/// <summary>
/// Валидатор фильтра '<see cref="PersonListFilter"/>'.
/// </summary>
public sealed class PersonListFilterValidator
    : AbstractValidator<PersonListFilter>
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="PersonListFilterValidator"/>.
    /// </summary>
    public PersonListFilterValidator()
    {
        AddNotEmptyOrWhiteSpaceRule(x => x.Name);
        AddNotEmptyOrWhiteSpaceRule(x => x.NameContains);
        AddNotEmptyOrWhiteSpaceRule(x => x.EmailContains);

        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage($"Некорректный формат '{nameof(PersonListFilter.Email)}'.");
    }

    private void AddNotEmptyOrWhiteSpaceRule(
        Expression<Func<PersonListFilter, string?>> expression)
    {
        var propertyName = ((MemberExpression)expression.Body).Member.Name;

        RuleFor(expression)
            .Must(value => value is null || !string.IsNullOrWhiteSpace(value))
            .WithMessage($"'{propertyName}' не может состоять из пробелов.");
    }
}
