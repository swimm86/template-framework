// ----------------------------------------------------------------------------------------------
// <copyright file="PersonSpecificationTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.Specification.Models;
using Template.Domain.Entities;
using Template.Getter.Application.Abstractions.Enums;
using Template.Getter.Application.Abstractions.Features.Person.List.Request;
using Template.Getter.Application.Specifications;

namespace Template.Getter.Application.Tests.Specifications;

/// <summary>
/// Тесты <see cref="PersonSpecification"/>.
/// Проверяют корректность фильтрации по <see cref="PersonListFilter.Email"/>,
/// применение <c>SortOptions</c> и структурные свойства спецификации.
/// </summary>
public sealed class PersonSpecificationTests
{
    /// <summary>
    /// <see cref="PersonSpecification.BuildOptions"/> при пустом запросе возвращает
    /// <see cref="QueryOptions{TEntity}"/> без фильтров.
    /// </summary>
    [Fact]
    public void BuildOptions_WithEmptyRequest_ReturnsOptionsWithoutFilters()
    {
        // Arrange
        var request = new PersonListRequest(DalPattern.UnitOfWork)
        {
            PageNumber = 1,
            PageSize = 10,
            Filter = null,
        };
        var specification = new PersonSpecification(request);

        // Act
        var options = specification.BuildOptions();

        // Assert
        options.Should().NotBeNull();
        options.Filters.Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="PersonSpecification.BuildOptions"/> при <c>Request.Filter = null</c>
    /// не добавляет фильтр по <c>Email</c>.
    /// </summary>
    [Fact]
    public void BuildOptions_WithNullFilter_DoesNotAddEmailFilter()
    {
        // Arrange
        var request = new PersonListRequest(DalPattern.Repository)
        {
            PageNumber = 1,
            PageSize = 10,
            Filter = null,
        };
        var specification = new PersonSpecification(request);

        // Act
        var options = specification.BuildOptions();

        // Assert
        options.Filters.Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="PersonSpecification.BuildOptions"/> при <c>Filter.Email = null</c>
    /// не добавляет фильтр по <c>Email</c>.
    /// </summary>
    [Fact]
    public void BuildOptions_WithNullFilterEmail_DoesNotAddEmailFilter()
    {
        // Arrange
        var request = new PersonListRequest(DalPattern.Repository)
        {
            PageNumber = 1,
            PageSize = 10,
            Filter = new PersonListFilter { Email = null },
        };
        var specification = new PersonSpecification(request);

        // Act
        var options = specification.BuildOptions();

        // Assert
        options.Filters.Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="PersonSpecification.BuildOptions"/> при пустом <c>Filter.Email</c>
    /// не добавляет фильтр по <c>Email</c>.
    /// </summary>
    [Fact]
    public void BuildOptions_WithEmptyFilterEmail_DoesNotAddEmailFilter()
    {
        // Arrange
        var request = new PersonListRequest(DalPattern.Repository)
        {
            PageNumber = 1,
            PageSize = 10,
            Filter = new PersonListFilter { Email = string.Empty },
        };
        var specification = new PersonSpecification(request);

        // Act
        var options = specification.BuildOptions();

        // Assert
        options.Filters.Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="PersonSpecification.BuildOptions"/> при <c>Filter.Email</c> из пробелов
    /// не добавляет фильтр по <c>Email</c>.
    /// </summary>
    [Fact]
    public void BuildOptions_WithWhitespaceFilterEmail_DoesNotAddEmailFilter()
    {
        // Arrange
        var request = new PersonListRequest(DalPattern.Repository)
        {
            PageNumber = 1,
            PageSize = 10,
            Filter = new PersonListFilter { Email = "   " },
        };
        var specification = new PersonSpecification(request);

        // Act
        var options = specification.BuildOptions();

        // Assert
        options.Filters.Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="PersonSpecification.BuildOptions"/> при заполненном <c>Filter.Email</c>
    /// добавляет фильтр по <c>Email</c> (без учёта регистра).
    /// </summary>
    [Fact]
    public void BuildOptions_WithFilterEmail_AddsCaseInsensitiveFilter()
    {
        // Arrange
        var request = new PersonListRequest(DalPattern.Repository)
        {
            PageNumber = 1,
            PageSize = 10,
            Filter = new PersonListFilter { Email = "Test@Example.com" },
        };
        var specification = new PersonSpecification(request);

        // Act
        var options = specification.BuildOptions();

        // Assert
        options.Filters.Should().ContainSingle();
    }

    /// <summary>
    /// <see cref="PersonSpecification.BuildOptions"/> применяет <c>SortOptions</c> из
    /// <see cref="PersonListRequest"/> в <see cref="QueryOptions{TEntity}"/>.
    /// </summary>
    [Fact]
    public void BuildOptions_AppliesSortOptionsFromRequest()
    {
        // Arrange
        var request = new PersonListRequest(DalPattern.Repository)
        {
            PageNumber = 1,
            PageSize = 10,
            SortOptions = ["Name.asc"],
        };
        var specification = new PersonSpecification(request);

        // Act
        var options = specification.BuildOptions();

        // Assert
        options.OrderBy.Should().NotBeEmpty();
    }

    /// <summary>
    /// <see cref="PersonSpecification.BuildOptions"/> возвращает непустой
    /// экземпляр <see cref="QueryOptions{TEntity}"/>.
    /// </summary>
    [Fact]
    public void BuildOptions_ReturnsOptions_NotNull()
    {
        // Arrange
        var request = new PersonListRequest(DalPattern.UnitOfWork);
        var specification = new PersonSpecification(request);

        // Act
        var options = specification.BuildOptions();

        // Assert
        options.Should().NotBeNull();
    }

    /// <summary>
    /// Конструктор <see cref="PersonSpecification"/> сохраняет переданный
    /// <see cref="PersonListRequest"/> в свойстве <see cref="PersonSpecification.Request"/>.
    /// </summary>
    [Fact]
    public void Specification_Constructor_SetsRequest()
    {
        // Arrange
        var request = new PersonListRequest(DalPattern.Repository);

        // Act
        var specification = new PersonSpecification(request);

        // Assert
        specification.Request.Should().BeSameAs(request);
    }

    /// <summary>
    /// <see cref="PersonSpecification"/> наследуется от
    /// <see cref="SpecificationBase{TEntity}"/>.
    /// </summary>
    [Fact]
    public void Specification_InheritsFromSpecificationBase()
    {
        // Arrange
        var specificationType = typeof(PersonSpecification);

        // Act
        var isAssignable = typeof(SpecificationBase<Person>).IsAssignableFrom(specificationType);

        // Assert
        isAssignable.Should().BeTrue();
    }
}
