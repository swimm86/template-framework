// ----------------------------------------------------------------------------------------------
// <copyright file="PageableRequestValidatorTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation.TestHelper;
using Shared.Application.Core.Dto.Requests.Validators;
using Shared.Application.Core.Tests.Support;

namespace Shared.Application.Core.Tests.Validators;

/// <summary>
/// Тесты для <see cref="PageableRequestValidator{TPageable}"/>.
/// </summary>
public sealed class PageableRequestValidatorTests
{
    /// <summary>
    /// Значения по умолчанию (<c>PageNumber = 1</c>, <c>PageSize = 100</c>) проходят валидацию.
    /// </summary>
    [Fact]
    public void Validate_WithDefaultValues_HasNoErrors()
    {
        // Arrange
        var request = new TestPageableRequest();
        var sut = new PageableRequestValidator<TestPageableRequest>();

        // Act
        var result = sut.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Положительные значения <c>PageNumber</c> и <c>PageSize</c> проходят валидацию.
    /// </summary>
    [Fact]
    public void Validate_WithPositiveValues_HasNoErrors()
    {
        // Arrange
        var request = new TestPageableRequest { PageNumber = 5, PageSize = 50 };
        var sut = new PageableRequestValidator<TestPageableRequest>();

        // Act
        var result = sut.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// <c>PageNumber &lt;= 0</c> порождает ошибку валидации.
    /// </summary>
    /// <param name="pageNumber">Проверяемое значение номера страницы.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Validate_PageNumberNotPositive_HasError(int pageNumber)
    {
        // Arrange
        var request = new TestPageableRequest { PageNumber = pageNumber };
        var sut = new PageableRequestValidator<TestPageableRequest>();

        // Act
        var result = sut.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorMessage(PageableRequestValidator<TestPageableRequest>.PageNumberMustBePositive);
    }

    /// <summary>
    /// <c>PageSize &lt;= 0</c> порождает ошибку валидации.
    /// </summary>
    /// <param name="pageSize">Проверяемое значение размера страницы.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Validate_PageSizeNotPositive_HasError(int pageSize)
    {
        // Arrange
        var request = new TestPageableRequest { PageSize = pageSize };
        var sut = new PageableRequestValidator<TestPageableRequest>();

        // Act
        var result = sut.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageSize)
            .WithErrorMessage(PageableRequestValidator<TestPageableRequest>.PageSizeMustBePositive);
    }

    /// <summary>
    /// Граничные положительные значения <c>PageNumber</c> и <c>PageSize</c>
    /// (включая <see cref="int.MaxValue"/>) проходят валидацию.
    /// </summary>
    [Fact]
    public void Validate_WithMaxPositiveValues_HasNoErrors()
    {
        // Arrange
        var request = new TestPageableRequest
        {
            PageNumber = int.MaxValue,
            PageSize = int.MaxValue,
        };
        var sut = new PageableRequestValidator<TestPageableRequest>();

        // Act
        var result = sut.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
