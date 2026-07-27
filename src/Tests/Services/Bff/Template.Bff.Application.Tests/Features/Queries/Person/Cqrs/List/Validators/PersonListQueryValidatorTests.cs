// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListQueryValidatorTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Template.Bff.Application.Features.Queries.Person.Cqrs.List;
using Template.Getter.Application.Abstractions.Enums;
using Template.Getter.Application.Abstractions.Features.Person.List.Validators;
using PersonListFilter = Template.Getter.Application.Abstractions.Features.Person.List.Request.PersonListFilter;
using PersonListQueryValidator = Template.Bff.Application.Features.Queries.Person.Cqrs.List.Validators.PersonListQueryValidator;
using PersonListRequest = Template.Bff.Application.Features.Queries.Person.Cqrs.List.Requests.PersonListRequest;

namespace Template.Bff.Application.Tests.Features.Queries.Person.Cqrs.List.Validators;

/// <summary>
/// Тесты поведения <see cref="PersonListQueryValidator"/> при проверке <see cref="PersonListQuery"/>.
/// </summary>
/// <remarks>
/// Тестируемый тип объявлен в пространстве имён
/// <c>Template.Bff.Application.Features.Queries.Person.Cqrs.List.Validators</c>.
/// </remarks>
public sealed class PersonListQueryValidatorTests
{
    /// <summary>
    /// Проверяет, что корректный запрос не порождает ошибок валидации.
    /// </summary>
    [Fact]
    public async Task Validate_WithValidRequest_DoesNotProduceErrors()
    {
        // Arrange
        var validator = CreateValidator();
        var query = CreateQuery(new PersonListFilter
        {
            Name = "Иван",
            NameContains = "Ив",
            Email = "ivan@example.com",
            EmailContains = "example",
        });

        // Act
        var result = await validator.ValidateAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет, что передача пустого запроса валидатору выбрасывает <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public async Task Validate_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var validator = CreateValidator();
        PersonListQuery? query = null;

        // Act
        Func<Task> act = () => validator.ValidateAsync(query!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Проверяет, что валидатор отклоняет запрос с пустым именем.
    /// </summary>
    [Fact]
    public async Task Validate_EmptyName_FailsWithNameError()
    {
        // Arrange
        var validator = CreateValidator();
        var query = CreateQuery(new PersonListFilter { Name = string.Empty });

        // Act
        var result = await validator.ValidateAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == $"{nameof(PersonListQuery.Request)}.{nameof(PersonListRequest.Filter)}.{nameof(PersonListFilter.Name)}" &&
            error.ErrorMessage == "'Name' не может состоять из пробелов.");
    }

    /// <summary>
    /// Проверяет, что валидатор отклоняет запрос с именем, состоящим только из пробелов.
    /// </summary>
    [Fact]
    public async Task Validate_WhitespaceOnlyName_FailsWithNameError()
    {
        // Arrange
        var validator = CreateValidator();
        var query = CreateQuery(new PersonListFilter { Name = "   " });

        // Act
        var result = await validator.ValidateAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == $"{nameof(PersonListQuery.Request)}.{nameof(PersonListRequest.Filter)}.{nameof(PersonListFilter.Name)}" &&
            error.ErrorMessage == "'Name' не может состоять из пробелов.");
    }

    /// <summary>
    /// Проверяет, что валидатор отклоняет запрос с некорректным адресом электронной почты.
    /// </summary>
    [Fact]
    public async Task Validate_InvalidEmail_FailsWithEmailError()
    {
        // Arrange
        var validator = CreateValidator();
        var query = CreateQuery(new PersonListFilter { Email = "not-an-email" });

        // Act
        var result = await validator.ValidateAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == $"{nameof(PersonListQuery.Request)}.{nameof(PersonListRequest.Filter)}.{nameof(PersonListFilter.Email)}" &&
            error.ErrorMessage == "Некорректный формат 'Email'.");
    }

    /// <summary>
    /// Проверяет, что валидатор отклоняет нулевой номер страницы.
    /// </summary>
    [Fact]
    public async Task Validate_PageNumberZero_FailsWithPageError()
    {
        // Arrange
        var validator = CreateValidator();
        var query = CreateQuery(pageNumber: 0);

        // Act
        var result = await validator.ValidateAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == $"{nameof(PersonListQuery.Request)}.{nameof(PersonListRequest.PageNumber)}" &&
            error.ErrorMessage == "Номер страницы должен быть не менее 1.");
    }

    /// <summary>
    /// Проверяет, что валидатор отклоняет отрицательный номер страницы.
    /// </summary>
    [Fact]
    public async Task Validate_PageNumberNegative_FailsWithPageError()
    {
        // Arrange
        var validator = CreateValidator();
        var query = CreateQuery(pageNumber: -1);

        // Act
        var result = await validator.ValidateAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == $"{nameof(PersonListQuery.Request)}.{nameof(PersonListRequest.PageNumber)}" &&
            error.ErrorMessage == "Номер страницы должен быть не менее 1.");
    }

    /// <summary>
    /// Проверяет, что валидатор отклоняет размер страницы, превышающий допустимый максимум.
    /// </summary>
    [Fact]
    public async Task Validate_PageSizeExceedsMax_FailsWithSizeError()
    {
        // Arrange
        var validator = CreateValidator();
        var query = CreateQuery(pageSize: 1001);

        // Act
        var result = await validator.ValidateAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == $"{nameof(PersonListQuery.Request)}.{nameof(PersonListRequest.PageSize)}" &&
            error.ErrorMessage == "Размер страницы не должен превышать 1000.");
    }

    /// <summary>
    /// Проверяет, что валидатор отклоняет недопустимый паттерн доступа к данным.
    /// </summary>
    [Fact]
    public async Task Validate_InvalidDalPattern_FailsWithDalPatternError()
    {
        // Arrange
        var validator = CreateValidator();
        var query = CreateQuery(dalPattern: (DalPattern)999);

        // Act
        var result = await validator.ValidateAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == $"{nameof(PersonListQuery.Request)}.{nameof(PersonListRequest.DalPattern)}" &&
            error.ErrorMessage == "Недопустимый паттерн доступа к данным.");
    }

    /// <summary>
    /// Проверяет, что валидатор отклоняет часть имени, состоящую только из пробелов.
    /// </summary>
    [Fact]
    public async Task Validate_WhitespaceOnlyNameContains_FailsWithNameContainsError()
    {
        // Arrange
        var validator = CreateValidator();
        var query = CreateQuery(new PersonListFilter { NameContains = "   " });

        // Act
        var result = await validator.ValidateAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == $"{nameof(PersonListQuery.Request)}.{nameof(PersonListRequest.Filter)}.{nameof(PersonListFilter.NameContains)}" &&
            error.ErrorMessage == "'NameContains' не может состоять из пробелов.");
    }

    /// <summary>
    /// Проверяет, что валидатор отклоняет часть адреса электронной почты, состоящую только из пробелов.
    /// </summary>
    [Fact]
    public async Task Validate_WhitespaceOnlyEmailContains_FailsWithEmailContainsError()
    {
        // Arrange
        var validator = CreateValidator();
        var query = CreateQuery(new PersonListFilter { EmailContains = "   " });

        // Act
        var result = await validator.ValidateAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == $"{nameof(PersonListQuery.Request)}.{nameof(PersonListRequest.Filter)}.{nameof(PersonListFilter.EmailContains)}" &&
            error.ErrorMessage == "'EmailContains' не может состоять из пробелов.");
    }

    /// <summary>
    /// Проверяет, что валидатор отклоняет неположительный размер страницы.
    /// </summary>
    /// <param name="pageSize">Проверяемый размер страницы.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validate_PageSizeZero_FailsWithSizeError(int pageSize)
    {
        // Arrange
        var validator = CreateValidator();
        var query = CreateQuery(pageSize: pageSize);

        // Act
        var result = await validator.ValidateAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == $"{nameof(PersonListQuery.Request)}.{nameof(PersonListRequest.PageSize)}" &&
            error.ErrorMessage == "Размер страницы должен быть больше 0.");
    }

    private static PersonListQueryValidator CreateValidator()
    {
        return new PersonListQueryValidator(
            new PersonListRequestValidator(new PersonListFilterValidator()));
    }

    private static PersonListQuery CreateQuery(
        PersonListFilter? filter = null,
        DalPattern dalPattern = DalPattern.Repository,
        int pageNumber = 1,
        int pageSize = 100)
    {
        var request = new PersonListRequest(dalPattern, UseCqrs: true)
        {
            Filter = filter,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        return new PersonListQuery(request);
    }
}
