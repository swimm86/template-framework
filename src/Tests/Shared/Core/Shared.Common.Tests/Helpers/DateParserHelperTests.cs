// ----------------------------------------------------------------------------------------------
// <copyright file="DateParserHelperTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Globalization;
using Shared.Common.Helpers;

namespace Shared.Common.Tests.Helpers;

/// <summary>
/// Тесты для вспомогательного класса <see cref="DateParserHelper"/>.
/// Проверяет корректность парсинга дат в различных форматах,
/// включая обработку null, пустых строк и невалидных значений.
/// </summary>
public sealed class DateParserHelperTests
{
    /// <summary>
    /// Тестовые данные для успешного парсинга DateTime из строки.
    /// Включает различные форматы дат и временных меток.
    /// </summary>
    public static TheoryData<string, DateTime> ValidDateTimeCases { get; } = new()
    {
        // Формат dd.MM.yyyy
        { "25.12.2023", new DateTime(2023, 12, 25) },
        { "01.01.2024", new DateTime(2024, 1, 1) },

        // Формат MM/dd/yyyy
        { "12/25/2023", new DateTime(2023, 12, 25) },
        { "1/1/2024", new DateTime(2024, 1, 1) },

        // Формат yyyy-MM-dd
        { "2023-12-25", new DateTime(2023, 12, 25) },
        { "2024-01-01", new DateTime(2024, 1, 1) },

        // Формат с временем dd.MM.yyyy HH:mm:ss
        { "25.12.2023 14:30:00", new DateTime(2023, 12, 25, 14, 30, 0) },
        { "01.01.2024 00:00:00", new DateTime(2024, 1, 1, 0, 0, 0) },

        // Формат M/d/yyyy h:mm:ss tt (AM/PM)
        { "12/25/2023 2:30:00 PM", new DateTime(2023, 12, 25, 14, 30, 0) },
        { "1/1/2024 12:00:00 AM", new DateTime(2024, 1, 1, 0, 0, 0) },

        // Формат с миллисекундами
        { "2023-12-25 14:30:45.123", new DateTime(2023, 12, 25, 14, 30, 45, 123) },
        { "25.12.2023 14:30:45.123", new DateTime(2023, 12, 25, 14, 30, 45, 123) },

        // Double representation (OADate).
        // Тест зависит от тройного fallback в TryParseDateTime:
        // TryParseExact → TryParse → TryParseDateFromDouble.
        // При изменении порядка fallback'ов ожидаемое значение может измениться.
        { "45285", new DateTime(2023, 12, 25) },
    };

    /// <summary>
    /// Тестовые данные для невалидных или пустых строк, которые должны вернуть null.
    /// </summary>
    public static TheoryData<string?> InvalidDateTimeCases { get; } =
    [
        (string?)null,
        "",
        "   ",
        "not a date",
        "99.99.9999",
        "2023-13-45"
    ];

    /// <summary>
    /// Проверяет успешный парсинг различных форматов DateTime.
    /// </summary>
    /// <param name="input">Входная строка с датой.</param>
    /// <param name="expected">Ожидаемое значение DateTime.</param>
    [Theory]
    [MemberData(nameof(ValidDateTimeCases))]
    public void TryParseDateTime_ValidDateString_ReturnsCorrectDateTime(
        string input,
        DateTime expected)
    {
        // Act
        var result = DateParserHelper.TryParseDateTime(input);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Проверяет, что невалидные строки возвращают null.
    /// </summary>
    /// <param name="input">Невалидная входная строка.</param>
    [Theory]
    [MemberData(nameof(InvalidDateTimeCases))]
    public void TryParseDateTime_InvalidString_ReturnsNull(string? input)
    {
        // Act
        var result = DateParserHelper.TryParseDateTime(input);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Проверяет обработку строк, начинающихся с одинарной кавычки.
    /// </summary>
    [Fact]
    public void TryParseDateTime_StringStartingWithQuote_StripsQuoteAndParses()
    {
        // Arrange
        const string input = "'25.12.2023";
        var expected = new DateTime(2023, 12, 25);

        // Act
        var result = DateParserHelper.TryParseDateTime(input);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Тестовые данные для успешного парсинга DateOnly.
    /// </summary>
    public static TheoryData<string, DateOnly> ValidDateOnlyCases { get; } = new()
    {
        { "25.12.2023", DateOnly.FromDateTime(new DateTime(2023, 12, 25)) },
        { "2023-12-25", DateOnly.FromDateTime(new DateTime(2023, 12, 25)) },
        { "12/25/2023", DateOnly.FromDateTime(new DateTime(2023, 12, 25)) },
        { "25.12.2023 14:30:00", DateOnly.FromDateTime(new DateTime(2023, 12, 25)) },
        { "2023-12-25 14:30:45.123", DateOnly.FromDateTime(new DateTime(2023, 12, 25)) },
    };

    /// <summary>
    /// Проверяет успешный парсинг DateOnly с игнорированием времени.
    /// </summary>
    /// <param name="input">Входная строка с датой.</param>
    /// <param name="expected">Ожидаемое значение DateOnly.</param>
    [Theory]
    [MemberData(nameof(ValidDateOnlyCases))]
    public void TryParseDateOnlyIgnoringTime_ValidDateString_ReturnsCorrectDateOnly(
        string input,
        DateOnly expected)
    {
        // Act
        var result = DateParserHelper.TryParseDateOnlyIgnoringTime(input);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Проверяет, что невалидные строки для DateOnly возвращают null.
    /// </summary>
    /// <param name="input">Невалидная входная строка.</param>
    [Theory]
    [MemberData(nameof(InvalidDateTimeCases))]
    public void TryParseDateOnlyIgnoringTime_InvalidString_ReturnsNull(string? input)
    {
        // Act
        var result = DateParserHelper.TryParseDateOnlyIgnoringTime(input);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Проверяет парсинг даты из double-представления (OADate).
    /// </summary>
    [Fact]
    public void TryParseDateTime_FromOADateDouble_ReturnsCorrectDateTime()
    {
        // Arrange
        const double oaDate = 45285.0; // 25 декабря 2023
        var expected = DateTime.FromOADate(oaDate);
        var input = oaDate.ToString(CultureInfo.InvariantCulture);

        // Act
        var result = DateParserHelper.TryParseDateTime(input);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Проверяет, что числовые строки вне диапазона OADate
    /// корректно возвращают null, а не падают с исключением.
    /// </summary>
    [Fact]
    public void TryParseDateTime_NumericValueOutsideOADateRange_ReturnsNull()
    {
        // Arrange
        const string hugeNumeric = "999999999999999999999999999999";

        // Act
        var result = DateParserHelper.TryParseDateTime(hugeNumeric);

        // Assert
        result.Should().BeNull();
    }
}
