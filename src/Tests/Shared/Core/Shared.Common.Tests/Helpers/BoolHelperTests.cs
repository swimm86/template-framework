// ----------------------------------------------------------------------------------------------
// <copyright file="BoolHelperTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Helpers;

namespace Shared.Common.Tests.Helpers;

/// <summary>
/// Тесты для вспомогательного класса <see cref="BoolHelper"/>.
/// Проверяет корректность преобразования строк в логические значения,
/// включая поддержку русских эквивалентов "да"/"нет".
/// </summary>
public sealed class BoolHelperTests
{
    /// <summary>
    /// Тестовые данные для проверки метода GetBooleanValueByString.
    /// Содержит различные варианты написания "да" и "нет" в разном регистре.
    /// </summary>
    public static TheoryData<string, bool, bool?> ValidBooleanStringCases { get; } = new()
    {
        // Точное совпадение (strong = false)
        { "да", false, true },
        { "ДА", false, true },
        { "Да", false, true },
        { "нет", false, false },
        { "НЕТ", false, false },
        { "Нет", false, false },

        // Поиск подстроки (strong = true)
        { "да", true, true },
        { "ДА!", true, null },
        { "конечно да", true, null },
        { "нет", true, false },
        { "НЕТ.", true, null },
        { "точно нет", true, null },
    };

    /// <summary>
    /// Тестовые данные для невалидных строк, которые должны вернуть null.
    /// </summary>
    public static TheoryData<string, bool> InvalidBooleanStringCases { get; } = new()
    {
        { "yes", false },
        { "no", false },
        { "true", false },
        { "false", false },
        { "1", false },
        { "0", false },
        { "maybe", false },
        { "", false },
        { "   ", false },
    };

    /// <summary>
    /// Проверяет корректное распознавание русских булевых строк "да"/"нет"
    /// при точном совпадении (strong = false).
    /// </summary>
    /// <param name="input">Входная строка для преобразования.</param>
    /// <param name="strong">Режим сравнения: точное совпадение или поиск подстроки.</param>
    /// <param name="expected">Ожидаемое логическое значение.</param>
    [Theory]
    [MemberData(nameof(ValidBooleanStringCases))]
    public void GetBooleanValueByString_ValidRussianBooleanString_ReturnsCorrectBooleanValue(
        string input,
        bool strong,
        bool? expected)
    {
        // Act
        var result = BoolHelper.GetBooleanValueByString(input, strong);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Проверяет, что невалидные строки возвращают null
    /// независимо от режима сравнения.
    /// </summary>
    /// <param name="input">Невалидная входная строка.</param>
    /// <param name="strong">Режим сравнения.</param>
    [Theory]
    [MemberData(nameof(InvalidBooleanStringCases))]
    public void GetBooleanValueByString_InvalidString_ReturnsNull(
        string input,
        bool strong)
    {
        // Act
        var result = BoolHelper.GetBooleanValueByString(input, strong);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Проверяет различие между режимами точного совпадения и поиска подстроки:
    /// в strong-режиме входная строка ищется внутри известных значений "да"/"нет".
    /// </summary>
    [Fact]
    public void GetBooleanValueByString_SearchTermContainedInKnownValue_DiffersBetweenStrongAndWeakModes()
    {
        // Arrange
        const string input = "д";

        // Act
        var exactMatchResult = BoolHelper.GetBooleanValueByString(input, strong: false);
        var substringResult = BoolHelper.GetBooleanValueByString(input, strong: true);

        // Assert
        exactMatchResult.Should().BeNull();
        substringResult.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что пробельные символы вокруг значения не игнорируются
    /// при точном совпадении (strong = false).
    /// </summary>
    /// <param name="input">Входная строка с пробелами.</param>
    /// <param name="strong">Режим сравнения.</param>
    [Theory]
    [InlineData(" да ", false)]
    [InlineData(" да ", true)]
    [InlineData(" нет ", false)]
    [InlineData(" нет ", true)]
    public void GetBooleanValueByString_WhitespaceAroundValue_ReturnsNull(
        string input,
        bool strong)
    {
        // Act
        var result = BoolHelper.GetBooleanValueByString(input, strong);

        // Assert
        result.Should().BeNull();
    }
}
