// ----------------------------------------------------------------------------------------------
// <copyright file="PageableRequestTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Tests.Support;
using Shared.Domain.Core.Dal;

namespace Shared.Application.Core.Tests;

/// <summary>
/// Тесты для <see cref="Shared.Application.Core.Dto.Requests.PageableRequest"/>.
/// </summary>
public sealed class PageableRequestTests
{
    /// <summary>
    /// Конструктор устанавливает значения по умолчанию: <c>PageNumber = 1</c>,
    /// <c>PageSize = 100</c>, <c>SortOptions = null</c>.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaults_ExposesPageNumber1AndPageSize100()
    {
        // Arrange & Act
        var request = new TestPageableRequest();

        // Assert
        request.PageNumber.Should().Be(1);
        request.PageSize.Should().Be(100);
        request.SortOptions.Should().BeNull();
    }

    /// <summary>
    /// <see cref="Shared.Application.Core.Dto.Requests.PageableRequest.ConvertSortOptions"/>
    /// возвращает ожидаемое количество элементов для разных входных данных.
    /// </summary>
    /// <param name="sortOptions">Список строк сортировки.</param>
    /// <param name="expectedCount">Ожидаемое количество распарсенных опций.</param>
    [Theory]
    [InlineData(null, 0)]
    [InlineData(new string[] { }, 0)]
    [InlineData(new[] { "Name.asc" }, 1)]
    [InlineData(new[] { "Name.asc", "Age.desc" }, 2)]
    [InlineData(new[] { "Complex.Property.Path.asc" }, 1)]
    [InlineData(new[] { "" }, 0)]
    [InlineData(new[] { "Name" }, 1)]
    [InlineData(new[] { "Name", "Age" }, 2)]
    [InlineData(new[] { "", "Name.asc", "" }, 1)]
    public void ConvertSortOptions_WithVariousInputs_ReturnsExpectedCount(string[]? sortOptions, int expectedCount)
    {
        // Arrange
        var request = new TestPageableRequest { SortOptions = sortOptions?.ToList() };

        // Act
        var result = request.ConvertSortOptions();

        // Assert
        result.Should().HaveCount(expectedCount);
    }

    /// <summary>
    /// <see cref="Shared.Application.Core.Dto.Requests.PageableRequest.ConvertSortOptions"/>
    /// корректно разбирает ключ, сложный путь свойств и направление сортировки.
    /// При отсутствии разделителя в строке вся строка используется как ключ,
    /// направление по умолчанию — <see cref="OrderDirectionType.Ascending"/>.
    /// </summary>
    [Fact]
    public void ConvertSortOptions_WithComplexInput_ParsesKeyAndDirection()
    {
        // Arrange
        var request = new TestPageableRequest
        {
            SortOptions = ["Name.asc", "User.Profile.Age.desc", "Id"],
        };

        // Act
        var result = request.ConvertSortOptions().ToList();

        // Assert
        result.Should().HaveCount(3);

        result[0].Key.Should().Be("Name");
        result[0].DirectionType.Should().Be(OrderDirectionType.Ascending);

        result[1].Key.Should().Be("User.Profile.Age");
        result[1].DirectionType.Should().Be(OrderDirectionType.Descending);

        result[2].Key.Should().Be("Id");
        result[2].DirectionType.Should().Be(OrderDirectionType.Ascending);
    }

    /// <summary>
    /// Направление сортировки парсится без учёта регистра (OData/JSON:API convention).
    /// </summary>
    /// <param name="input">Строка сортировки с направлением в произвольном регистре.</param>
    /// <param name="expectedDirection">Ожидаемое направление.</param>
    [Theory]
    [InlineData("Name.asc", OrderDirectionType.Ascending)]
    [InlineData("Name.ASC", OrderDirectionType.Ascending)]
    [InlineData("Name.Asc", OrderDirectionType.Ascending)]
    [InlineData("Name.desc", OrderDirectionType.Descending)]
    [InlineData("Name.DESC", OrderDirectionType.Descending)]
    [InlineData("Name.Desc", OrderDirectionType.Descending)]
    [InlineData("Name.DeSc", OrderDirectionType.Descending)]
    public void ConvertSortOptions_DirectionIsCaseInsensitive(string input, OrderDirectionType expectedDirection)
    {
        // Arrange
        var request = new TestPageableRequest { SortOptions = [input] };

        // Act
        var result = request.ConvertSortOptions().ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Key.Should().Be("Name");
        result[0].DirectionType.Should().Be(expectedDirection);
    }

    /// <summary>
    /// <see cref="Shared.Application.Core.Dto.Requests.PageableRequest.ConvertSortOptions"/>
    /// сохраняет порядок элементов входной коллекции.
    /// </summary>
    [Fact]
    public void ConvertSortOptions_PreservesInputOrder()
    {
        // Arrange
        var request = new TestPageableRequest
        {
            SortOptions = ["Zebra.asc", "Apple.desc", "Mango"],
        };

        // Act
        var result = request.ConvertSortOptions().ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Key.Should().Be("Zebra");
        result[1].Key.Should().Be("Apple");
        result[2].Key.Should().Be("Mango");
    }

    /// <summary>
    /// Дублирующиеся ключи с разными направлениями сохраняются —
    /// дедупликация не выполняется (ответственность лежит на уровне ниже).
    /// </summary>
    [Fact]
    public void ConvertSortOptions_DuplicateKeysAreKept()
    {
        // Arrange
        var request = new TestPageableRequest
        {
            SortOptions = ["Name.asc", "Name.desc", "Name"],
        };

        // Act
        var result = request.ConvertSortOptions().ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Key.Should().Be("Name");
        result[0].DirectionType.Should().Be(OrderDirectionType.Ascending);
        result[1].Key.Should().Be("Name");
        result[1].DirectionType.Should().Be(OrderDirectionType.Descending);
        result[2].Key.Should().Be("Name");
        result[2].DirectionType.Should().Be(OrderDirectionType.Ascending);
    }

    /// <summary>
    /// Пустые строки в коллекции игнорируются, валидные записи обрабатываются.
    /// </summary>
    [Fact]
    public void ConvertSortOptions_EmptyEntriesAreFiltered()
    {
        // Arrange
        var request = new TestPageableRequest
        {
            SortOptions = ["", "Name.asc", "", "Age.desc", ""],
        };

        // Act
        var result = request.ConvertSortOptions().ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Key.Should().Be("Name");
        result[1].Key.Should().Be("Age");
    }

    /// <summary>
    /// Префиксные и постфиксные пробелы вокруг ключа и направления отбрасываются.
    /// </summary>
    [Fact]
    public void ConvertSortOptions_TrimsPrefixAndSuffixWhitespace()
    {
        // Arrange
        var request = new TestPageableRequest
        {
            SortOptions = ["  Name.asc  ", "  Age  .  desc  ", "   Id   "],
        };

        // Act
        var result = request.ConvertSortOptions().ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Key.Should().Be("Name");
        result[0].DirectionType.Should().Be(OrderDirectionType.Ascending);
        result[1].Key.Should().Be("Age");
        result[1].DirectionType.Should().Be(OrderDirectionType.Descending);
        result[2].Key.Should().Be("Id");
        result[2].DirectionType.Should().Be(OrderDirectionType.Ascending);
    }

    /// <summary>
    /// <see cref="Shared.Application.Core.Dto.Requests.PageableRequest.ConvertSortOptions"/>
    /// выбрасывает <see cref="ArgumentException"/>, если строка содержит разделитель,
    /// но последний сегмент не соответствует ни одному элементу <see cref="OrderDirectionType"/>.
    /// </summary>
    [Fact]
    public void ConvertSortOptions_WithInvalidDirection_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestPageableRequest
        {
            SortOptions = ["Name.invalid"],
        };

        // Act
        var act = () => request.ConvertSortOptions();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// <see cref="Shared.Application.Core.Dto.Requests.PageableRequest.ConvertSortOptions"/>
    /// выбрасывает <see cref="ArgumentException"/>, если строка состоит только из разделителя
    /// (ключ оказывается пустым).
    /// </summary>
    [Fact]
    public void ConvertSortOptions_OnlyDelimiter_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestPageableRequest
        {
            SortOptions = ["."],
        };

        // Act
        var act = () => request.ConvertSortOptions();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// <see cref="Shared.Application.Core.Dto.Requests.PageableRequest.ConvertSortOptions"/>
    /// выбрасывает <see cref="ArgumentException"/>, если строка содержит разделитель,
    /// но после него ничего не указано (трейлинг-точка).
    /// </summary>
    [Fact]
    public void ConvertSortOptions_TrailingDelimiter_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestPageableRequest
        {
            SortOptions = ["Name."],
        };

        // Act
        var act = () => request.ConvertSortOptions();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// <see cref="Shared.Application.Core.Dto.Requests.PageableRequest.ConvertSortOptions"/>
    /// возвращает пустую коллекцию, если все элементы
    /// <see cref="Shared.Application.Core.Dto.Requests.PageableRequest.SortOptions"/>
    /// состоят из пробелов.
    /// </summary>
    [Fact]
    public void ConvertSortOptions_WhitespaceOnlyEntries_AreFiltered()
    {
        // Arrange
        var request = new TestPageableRequest
        {
            SortOptions = [" ", "   ", "\t"],
        };

        // Act
        var result = request.ConvertSortOptions().ToList();

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="Shared.Application.Core.Dto.Requests.PageableRequest.ConvertSortOptions"/>
    /// отклоняет вход с пустым ключом (только разделитель и направление),
    /// так как сортировка по пустому ключу не имеет смысла.
    /// </summary>
    [Fact]
    public void ConvertSortOptions_EmptyKeyWithValidDirection_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestPageableRequest
        {
            SortOptions = [".asc"],
        };

        // Act
        var act = () => request.ConvertSortOptions();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// <c>null</c>-элементы внутри коллекции
    /// <see cref="Shared.Application.Core.Dto.Requests.PageableRequest.SortOptions"/>
    /// отбрасываются как невалидные (фильтр работает на <c>null</c>).
    /// </summary>
    [Fact]
    public void ConvertSortOptions_NullEntries_AreFiltered()
    {
        // Arrange
        var request = new TestPageableRequest
        {
            SortOptions = ["Name.asc", null!, "Age.desc"],
        };

        // Act
        var result = request.ConvertSortOptions().ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Key.Should().Be("Name");
        result[1].Key.Should().Be("Age");
    }

    /// <summary>
    /// Префиксные и постфиксные пробелы вокруг всего входа и вокруг сегментов
    /// после <c>Split</c> отбрасываются перед валидацией направления.
    /// </summary>
    [Fact]
    public void ConvertSortOptions_TrimmedInvalidDirection_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestPageableRequest
        {
            SortOptions = ["   Name.invalid   "],
        };

        // Act
        var act = () => request.ConvertSortOptions();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// При наличии хотя бы одной невалидной записи среди прочих валидных
    /// метод <see cref="Shared.Application.Core.Dto.Requests.PageableRequest.ConvertSortOptions"/>
    /// выбрасывает исключение и не возвращает частичный результат (fail-fast).
    /// </summary>
    [Fact]
    public void ConvertSortOptions_MixedValidAndInvalid_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestPageableRequest
        {
            SortOptions = ["Name.asc", "Age.invalid"],
        };

        // Act
        var act = () => request.ConvertSortOptions().ToList();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// <see cref="Shared.Application.Core.Dto.Requests.PageableRequest.ConvertSortOptions"/>
    /// выбрасывает <see cref="ArgumentException"/>, если сегмент направления после
    /// <c>Trim</c> оказывается пустым (whitespace-only вход).
    /// </summary>
    /// <param name="input">Строка сортировки с whitespace-only сегментом направления.</param>
    [Theory]
    [InlineData("Name.   ")]
    [InlineData("Name.\t")]
    [InlineData("Name. ")]
    [InlineData("Name.\r\n")]
    public void ConvertSortOptions_WithWhitespaceOnlyDirection_ThrowsArgumentException(string input)
    {
        // Arrange
        var request = new TestPageableRequest { SortOptions = [input] };

        // Act
        var act = () => request.ConvertSortOptions();

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
