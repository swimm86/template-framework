// ----------------------------------------------------------------------------------------------
// <copyright file="PersonValidatorTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using PersonValidator = Template.Setter.Application.Features.Person.Validators.PersonValidator;

namespace Template.Setter.Application.Tests.Features.Person.Validators;

/// <summary>
/// Тесты <see cref="PersonValidator"/>.
/// Проверяют правила для <see cref="global::Template.Domain.Entities.Person.Name"/>
/// и <see cref="global::Template.Domain.Entities.Person.Email"/>.
/// </summary>
public sealed class PersonValidatorTests
{
    /// <summary>
    /// Корректная <see cref="global::Template.Domain.Entities.Person"/> не порождает ошибок валидации.
    /// </summary>
    [Fact]
    public void Validate_ValidPerson_NoErrors()
    {
        // Arrange
        var validator = new PersonValidator();
        var person = Domain.Entities.Person.Create("John", "john@example.com");

        // Act
        var result = validator.Validate(person);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Пустое <see cref="global::Template.Domain.Entities.Person.Name"/> порождает ошибку <c>NotEmpty</c>.
    /// </summary>
    [Fact]
    public void Validate_EmptyName_HasNotEmptyError()
    {
        // Arrange
        var validator = new PersonValidator();
        var person = Domain.Entities.Person.Create(string.Empty, "john@example.com");

        // Act
        var result = validator.Validate(person);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(Domain.Entities.Person.Name) &&
            e.ErrorMessage.Contains("не может быть пустым"));
    }

    /// <summary>
    /// <see cref="global::Template.Domain.Entities.Person.Name"/>, начинающееся со строчной буквы, порождает ошибку о регистре.
    /// </summary>
    [Fact]
    public void Validate_LowerCaseName_HasUpperCaseError()
    {
        // Arrange
        var validator = new PersonValidator();
        var person = Domain.Entities.Person.Create("john", "john@example.com");

        // Act
        var result = validator.Validate(person);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(Domain.Entities.Person.Name) &&
            e.ErrorMessage.Contains("заглавной"));
    }

    /// <summary>
    /// <see cref="global::Template.Domain.Entities.Person.Name"/>, содержащее цифры, порождает ошибку о цифрах.
    /// </summary>
    [Fact]
    public void Validate_NameWithDigits_HasNoDigitsError()
    {
        // Arrange
        var validator = new PersonValidator();
        var person = Domain.Entities.Person.Create("John1", "john@example.com");

        // Act
        var result = validator.Validate(person);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(Domain.Entities.Person.Name) &&
            e.ErrorMessage.Contains("цифр"));
    }

    /// <summary>
    /// Пустой <see cref="global::Template.Domain.Entities.Person.Email"/> порождает ошибку <c>NotEmpty</c>.
    /// </summary>
    [Fact]
    public void Validate_EmptyEmail_HasNotEmptyError()
    {
        // Arrange
        var validator = new PersonValidator();
        var person = Domain.Entities.Person.Create("John", string.Empty);

        // Act
        var result = validator.Validate(person);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(Domain.Entities.Person.Email) &&
            e.ErrorMessage.Contains("не может быть пустым"));
    }

    /// <summary>
    /// Некорректный формат <see cref="global::Template.Domain.Entities.Person.Email"/> порождает ошибку <c>EmailAddress</c>.
    /// </summary>
    [Fact]
    public void Validate_InvalidEmailFormat_HasEmailAddressError()
    {
        // Arrange
        var validator = new PersonValidator();
        var person = Domain.Entities.Person.Create("John", "not-an-email");

        // Act
        var result = validator.Validate(person);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(Domain.Entities.Person.Email) &&
            e.ErrorMessage.Contains("Некорректный формат"));
    }

    /// <summary>
    /// <see cref="global::Template.Domain.Entities.Person.Name"/>, начинающееся с заглавной буквы, не порождает ошибок.
    /// </summary>
    [Fact]
    public void Validate_UpperCaseName_NoErrors()
    {
        // Arrange
        var validator = new PersonValidator();
        var person = Domain.Entities.Person.Create("John", "john@example.com");

        // Act
        var result = validator.Validate(person);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Корректный <see cref="global::Template.Domain.Entities.Person.Email"/> не порождает ошибок.
    /// </summary>
    [Fact]
    public void Validate_ValidEmail_NoErrors()
    {
        // Arrange
        var validator = new PersonValidator();
        var person = Domain.Entities.Person.Create("John", "valid@example.com");

        // Act
        var result = validator.Validate(person);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
