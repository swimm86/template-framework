// ----------------------------------------------------------------------------------------------
// <copyright file="DateOnlyConverterTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text.Json;
using Shared.Common.JsonConverters;

namespace Shared.Common.Tests.JsonConverters;

/// <summary>
/// Тесты для <see cref="DateOnlyConverter"/> и <see cref="NullableDateOnlyConverter"/>.
/// </summary>
public sealed class DateOnlyConverterTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        Converters = { new DateOnlyConverter(), new NullableDateOnlyConverter() },
    };

    /// <summary>
    /// Десериализация валидной строки возвращает корректный <see cref="DateOnly"/>.
    /// </summary>
    [Fact]
    public void Read_ValidDateOnly_ReturnsCorrectValue()
    {
        // Arrange
        var json = "\"31.12.2023\"";
        var expected = new DateOnly(2023, 12, 31);

        // Act
        var result = JsonSerializer.Deserialize<DateOnly>(json, _options);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Десериализация nullable DateOnly возвращает корректное значение.
    /// </summary>
    [Fact]
    public void Read_NullableValidDateOnly_ReturnsCorrectValue()
    {
        // Arrange
        var json = "\"15.06.2024\"";
        var expected = new DateOnly(2024, 06, 15);

        // Act
        var result = JsonSerializer.Deserialize<DateOnly?>(json, _options);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Десериализация null в nullable DateOnly возвращает null.
    /// </summary>
    [Fact]
    public void Read_NullableNullToken_ReturnsNull()
    {
        // Arrange
        var json = "null";

        // Act
        var result = JsonSerializer.Deserialize<DateOnly?>(json, _options);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Сериализация DateOnly записывает в ожидаемом формате.
    /// </summary>
    [Fact]
    public void Write_DateOnly_WritesIsoFormat()
    {
        // Arrange
        var value = new DateOnly(2023, 12, 31);

        // Act
        var json = JsonSerializer.Serialize(value, _options);

        // Assert
        json.Should().Be("\"31.12.2023\"");
    }

    /// <summary>
    /// Сериализация nullable DateOnly (не null) записывает значение.
    /// </summary>
    [Fact]
    public void Write_NullableDateOnly_NonNull_WritesIsoFormat()
    {
        // Arrange
        DateOnly? value = new DateOnly(2024, 01, 15);

        // Act
        var json = JsonSerializer.Serialize(value, _options);

        // Assert
        json.Should().Be("\"15.01.2024\"");
    }

    /// <summary>
    /// Сериализация nullable DateOnly (null) записывает null.
    /// </summary>
    [Fact]
    public void Write_NullableDateOnly_Null_WritesNull()
    {
        // Arrange
        DateOnly? value = null;

        // Act
        var json = JsonSerializer.Serialize(value, _options);

        // Assert
        json.Should().Be("null");
    }

    /// <summary>
    /// Неверный формат даты вызывает <see cref="FormatException"/>.
    /// </summary>
    [Fact]
    public void Read_InvalidFormat_Throws()
    {
        // Arrange
        var json = "\"2023-12-31\"";

        // Act
        var act = () => JsonSerializer.Deserialize<DateOnly>(json, _options);

        // Assert
        act.Should().Throw<FormatException>();
    }

    /// <summary>
    /// Round-trip: сериализация и десериализация возвращает исходное значение.
    /// </summary>
    [Fact]
    public void RoundTrip_Value_EqualsOriginal()
    {
        // Arrange
        var original = new DateOnly(2025, 05, 18);

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<DateOnly>(json, _options);

        // Assert
        deserialized.Should().Be(original);
    }

    /// <summary>
    /// Round-trip: nullable значение после цикла сохраняется.
    /// </summary>
    [Fact]
    public void RoundTrip_NullableValue_EqualsOriginal()
    {
        // Arrange
        DateOnly? original = new DateOnly(2025, 12, 25);

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<DateOnly?>(json, _options);

        // Assert
        deserialized.Should().Be(original);
    }

    /// <summary>
    /// Round-trip: null после цикла остаётся null.
    /// </summary>
    [Fact]
    public void RoundTrip_NullableNull_ReturnsNull()
    {
        // Arrange
        DateOnly? original = null;

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<DateOnly?>(json, _options);

        // Assert
        deserialized.Should().BeNull();
    }
}
