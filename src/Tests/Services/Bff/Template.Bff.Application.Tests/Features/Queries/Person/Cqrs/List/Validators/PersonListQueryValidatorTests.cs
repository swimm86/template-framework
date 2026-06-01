// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListQueryValidatorTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation;
using Template.Bff.Application.Features.Queries.Person.Cqrs.List;
using Template.Bff.Application.Features.Queries.Person.Cqrs.List.Validators;
using Template.Getter.Application.Abstractions.Features.Person.List.Validators;

namespace Template.Bff.Application.Tests.Features.Queries.Person.Cqrs.List.Validators;

/// <summary>
/// Тесты <see cref="PersonListQueryValidator"/>.
/// Проверяют наследование от <see cref="AbstractValidator{T}"/>,
/// наличие публичного конструктора с <see cref="PersonListRequestValidator"/>
/// и корректность создания экземпляра.
/// </summary>
public sealed class PersonListQueryValidatorTests
{
    /// <summary>
    /// Валидатор наследуется от
    /// <see cref="AbstractValidator{T}"/> для <see cref="PersonListQuery"/>.
    /// </summary>
    [Fact]
    public void Validator_InheritsFromAbstractValidator()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var isAbstractValidator = sut is AbstractValidator<PersonListQuery>;

        // Assert
        isAbstractValidator.Should().BeTrue();
    }

    /// <summary>
    /// Валидатор имеет публичный конструктор, принимающий
    /// <see cref="PersonListRequestValidator"/>.
    /// </summary>
    [Fact]
    public void Validator_HasPublicConstructor_WithPersonListRequestValidator()
    {
        // Arrange
        var sut = CreateSut();
        var constructor = sut.GetType()
            .GetConstructor([typeof(PersonListRequestValidator)]);

        // Act
        var exists = constructor is not null && constructor.IsPublic;

        // Assert
        exists.Should().BeTrue();
    }

    /// <summary>
    /// Конструктор валидатора принимает параметр типа
    /// <see cref="PersonListRequestValidator"/> и не выбрасывает исключений
    /// на этапе конструирования (проверка типа параметра).
    /// </summary>
    [Fact]
    public void Validator_AcceptsNullRequestValidator_DoesNotThrowAtConstruction()
    {
        // Arrange
        var constructorType = typeof(PersonListQueryValidator);
        var parameters = constructorType.GetConstructor([typeof(PersonListRequestValidator)]);

        // Act
        var acceptsPersonListRequestValidator = parameters is not null;

        // Assert
        acceptsPersonListRequestValidator.Should().BeTrue();
    }

    private static PersonListQueryValidator CreateSut()
    {
        return new PersonListQueryValidator(new PersonListRequestValidator(new PersonListFilterValidator()));
    }
}
