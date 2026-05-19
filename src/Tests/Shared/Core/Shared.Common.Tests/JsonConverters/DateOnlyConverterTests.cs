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

    [Fact]
    public void Read_ValidDateOnly_ReturnsCorrectValue()
    {
        var json = "\"31.12.2023\"";
        var expected = new DateOnly(2023, 12, 31);

        var result = JsonSerializer.Deserialize<DateOnly>(json, _options);

        result.Should().Be(expected);
    }

    [Fact]
    public void Read_NullableValidDateOnly_ReturnsCorrectValue()
    {
        var json = "\"15.06.2024\"";
        var expected = new DateOnly(2024, 06, 15);

        var result = JsonSerializer.Deserialize<DateOnly?>(json, _options);

        result.Should().Be(expected);
    }

    [Fact]
    public void Read_NullableNullToken_ReturnsNull()
    {
        var json = "null";

        var result = JsonSerializer.Deserialize<DateOnly?>(json, _options);

        result.Should().BeNull();
    }

    [Fact]
    public void Write_DateOnly_WritesIsoFormat()
    {
        var value = new DateOnly(2023, 12, 31);

        var json = JsonSerializer.Serialize(value, _options);

        json.Should().Be("\"31.12.2023\"");
    }

    [Fact]
    public void Write_NullableDateOnly_NonNull_WritesIsoFormat()
    {
        DateOnly? value = new DateOnly(2024, 01, 15);

        var json = JsonSerializer.Serialize(value, _options);

        json.Should().Be("\"15.01.2024\"");
    }

    [Fact]
    public void Write_NullableDateOnly_Null_WritesNull()
    {
        DateOnly? value = null;

        var json = JsonSerializer.Serialize(value, _options);

        json.Should().Be("null");
    }

    [Fact]
    public void Read_InvalidFormat_Throws()
    {
        var json = "\"2023-12-31\"";

        var act = () => JsonSerializer.Deserialize<DateOnly>(json, _options);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void RoundTrip_Value_EqualsOriginal()
    {
        var original = new DateOnly(2025, 05, 18);

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<DateOnly>(json, _options);

        deserialized.Should().Be(original);
    }

    [Fact]
    public void RoundTrip_NullableValue_EqualsOriginal()
    {
        DateOnly? original = new DateOnly(2025, 12, 25);

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<DateOnly?>(json, _options);

        deserialized.Should().Be(original);
    }

    [Fact]
    public void RoundTrip_NullableNull_ReturnsNull()
    {
        DateOnly? original = null;

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<DateOnly?>(json, _options);

        deserialized.Should().BeNull();
    }
}
