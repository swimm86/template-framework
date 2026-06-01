// ----------------------------------------------------------------------------------------------
// <copyright file="PersonValidator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation;

namespace Template.Setter.Application.Features.Person.Validators;

/// <summary>
/// Валидатор сущности "Персона".
/// </summary>
public sealed class PersonValidator
    : AbstractValidator<Domain.Entities.Person>
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="PersonValidator"/>.
    /// </summary>
    public PersonValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage($"{nameof(Domain.Entities.Person.Name)} не может быть пустым.")
            .Must(name => name.Length > 0 && char.IsUpper(name[0]))
            .WithMessage($"{nameof(Domain.Entities.Person.Name)} должен начинаться с заглавной буквы.")
            .Must(name => !name.Any(char.IsDigit))
            .WithMessage($"{nameof(Domain.Entities.Person.Name)} не должен содержать цифр.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage($"{nameof(Domain.Entities.Person.Email)} не может быть пустым.")
            .EmailAddress()
            .WithMessage($"Некорректный формат {nameof(Domain.Entities.Person.Email)}.");
    }
}
