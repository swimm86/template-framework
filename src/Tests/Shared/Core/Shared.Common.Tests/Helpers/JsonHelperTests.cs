// ----------------------------------------------------------------------------------------------
// <copyright file="JsonHelperTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text.Json;
using Shared.Common.Helpers;

namespace Shared.Common.Tests.Helpers;

/// <summary>
/// Тесты для вспомогательного класса <see cref="JsonHelper"/>.
/// Проверяет корректность десериализации JSON с обработкой ошибок.
/// </summary>
public sealed class JsonHelperTests
{
    /// <summary>
    /// Тестовый класс для сериализации/десериализации.
    /// </summary>
    public class TestDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string>? Tags { get; set; }
    }

    /// <summary>
    /// Тестовые данные для успешной десериализации.
    /// </summary>
    public static TheoryData<string, TestDto> ValidJsonCases { get; } = new()
    {
        {
            "{\"Id\": 1, \"Name\": \"Test\", \"CreatedAt\": \"2023-12-25T14:30:00\", \"Tags\": [\"tag1\", \"tag2\"]}",
            new TestDto
            {
                Id = 1,
                Name = "Test",
                CreatedAt = new DateTime(2023, 12, 25, 14, 30, 0),
                Tags = ["tag1", "tag2"]
            }
        },
        {
            "{\"Id\": 42, \"Name\": null, \"CreatedAt\": \"2024-01-01T00:00:00\"}",
            new TestDto
            {
                Id = 42,
                Name = null,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0),
                Tags = null
            }
        },
        {
            "{}",
            new TestDto
            {
                Id = 0,
                Name = null,
                CreatedAt = default,
                Tags = null
            }
        },
    };

    /// <summary>
    /// Тестовые данные для невалидного JSON.
    /// </summary>
    public static TheoryData<string> InvalidJsonCases { get; } =
    [
        "not a json",
        "{ invalid json }",
        "{\"Id\": 1, \"Name\": \"Test\"", // незакрытая скобка
        "[1, 2, 3]", // массив вместо объекта
        "null",
        ""
    ];

    /// <summary>
    /// Проверяет успешную десериализацию валидного JSON.
    /// </summary>
    /// <param name="json">Входная JSON-строка.</param>
    /// <param name="expected">Ожидаемый объект после десериализации.</param>
    [Theory]
    [MemberData(nameof(ValidJsonCases))]
    public void TryDeserialize_ValidJson_ReturnsTrueAndCorrectObject(
        string json,
        TestDto expected)
    {
        // Act
        var result = JsonHelper.TryDeserialize(json, out TestDto? actual);

        // Assert
        result.Should().BeTrue();
        actual.Should().BeEquivalentTo(expected);

    }

    /// <summary>
    /// Проверяет, что невалидный JSON возвращает false и null.
    /// </summary>
    /// <param name="json">Невалидная JSON-строка.</param>
    [Theory]
    [MemberData(nameof(InvalidJsonCases))]
    public void TryDeserialize_InvalidJson_ReturnsFalseAndNull(string json)
    {
        // Act
        var result = JsonHelper.TryDeserialize(json, out TestDto? actual);

        // Assert
        result.Should().BeFalse();
        actual.Should().BeNull();
    }

    /// <summary>
    /// Проверяет десериализацию с кастомными JsonSerializerOptions.
    /// </summary>
    [Fact]
    public void TryDeserialize_WithCustomOptions_UsesProvidedOptions()
    {
        // Arrange
        const string json = "{\"id\": 1, \"name\": \"Test\"}";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        var result = JsonHelper.TryDeserialize(json, out TestDto? actual, options);

        // Assert
        result.Should().BeTrue();
        actual.Should().BeEquivalentTo(new TestDto
        {
            Id = 1,
            Name = "Test"
        });
    }

    /// <summary>
    /// Проверяет десериализацию null-входа.
    /// </summary>
    [Fact]
    public void TryDeserialize_NullJson_ReturnsFalseAndNull()
    {
        // Act
        var result = JsonHelper.TryDeserialize(null!, out TestDto? actual);

        // Assert
        result.Should().BeFalse();
        actual.Should().BeNull();
    }

    /// <summary>
    /// Проверяет десериализацию пустой строки.
    /// </summary>
    [Fact]
    public void TryDeserialize_EmptyString_ReturnsFalseAndNull()
    {
        // Act
        var result = JsonHelper.TryDeserialize("", out TestDto? actual);

        // Assert
        result.Should().BeFalse();
        actual.Should().BeNull();
    }

    /// <summary>
    /// Проверяет десериализацию JSON с неожиданным типом данных.
    /// </summary>
    [Fact]
    public void TryDeserialize_WrongType_ReturnsFalseAndNull()
    {
        // Arrange - пытаемся десериализовать число как объект
        const string json = "123";

        // Act
        var result = JsonHelper.TryDeserialize(json, out TestDto? actual);

        // Assert
        result.Should().BeFalse();
        actual.Should().BeNull();
    }

    /// <summary>
    /// Проверяет десериализацию простого типа (string).
    /// </summary>
    [Fact]
    public void TryDeserialize_SimpleType_String_ReturnsCorrectValue()
    {
        // Arrange
        const string json = "\"Hello World\"";

        // Act
        var result = JsonHelper.TryDeserialize(json, out string? actual);

        // Assert
        result.Should().BeTrue();
        actual.Should().Be("Hello World");
    }

    /// <summary>
    /// Проверяет десериализацию списка объектов.
    /// </summary>
    [Fact]
    public void TryDeserialize_ListOfObjects_ReturnsCorrectList()
    {
        // Arrange
        const string json = "[{\"Id\": 1, \"Name\": \"First\"}, {\"Id\": 2, \"Name\": \"Second\"}]";

        // Act
        var result = JsonHelper.TryDeserialize(json, out List<TestDto>? actual);

        // Assert
        result.Should().BeTrue();
        actual.Should().BeEquivalentTo(
        [
            new TestDto { Id = 1, Name = "First" },
            new TestDto { Id = 2, Name = "Second" }
        ]);
    }

    /// <summary>
    /// Проверяет, что метод ловит JsonException.
    /// </summary>
    [Fact]
    public void TryDeserialize_MalformedJson_CatchesJsonException()
    {
        // Arrange - экранированные кавычки делают JSON невалидным
        const string json = """{\"Id\": 1}""";

        // Act
        var result = JsonHelper.TryDeserialize(json, out TestDto? actual);

        // Assert
        result.Should().BeFalse();
        actual.Should().BeNull();
    }
}
