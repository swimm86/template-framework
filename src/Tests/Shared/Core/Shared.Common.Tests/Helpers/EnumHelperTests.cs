// ----------------------------------------------------------------------------------------------
// <copyright file="EnumHelperTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.ComponentModel;
using Shared.Common.Helpers;

namespace Shared.Common.Tests.Helpers;

/// <summary>
/// Тесты для вспомогательного класса <see cref="EnumHelper"/>.
/// Проверяет корректность получения значений перечислений по атрибуту Description.
/// </summary>
public sealed class EnumHelperTests
{
    /// <summary>
    /// Тестовое перечисление с Description-атрибутами для проверки.
    /// </summary>
    private enum TestEnumWithDescriptions
    {
        [Description("Активный статус")]
        Active,

        [Description("Неактивный статус")]
        Inactive,

        [Description("Ожидание подтверждения")]
        Pending,

        WithoutDescription
    }

    /// <summary>
    /// Тестовое перечисление без Description-атрибутов.
    /// </summary>
    private enum TestEnumWithoutDescriptions
    {
        Value1,
        Value2,
        Value3
    }

    /// <summary>
    /// Тестовые данные для успешного поиска по Description.
    /// </summary>
    public static TheoryData<string, TestEnumWithDescriptions> ValidDescriptionCases { get; } = new()
    {
        { "Активный статус", TestEnumWithDescriptions.Active },
        { "активный статус", TestEnumWithDescriptions.Active }, // игнорирование регистра
        { "АКТИВНЫЙ СТАТУС", TestEnumWithDescriptions.Active },
        { "Неактивный статус", TestEnumWithDescriptions.Inactive },
        { "Ожидание подтверждения", TestEnumWithDescriptions.Pending },
    };

    /// <summary>
    /// Тестовые данные для невалидных описаний.
    /// </summary>
    public static TheoryData<string?> InvalidDescriptionCases { get; } = new()
    {
        { null },
        { "" },
        { "   " },
        { "Несуществующее описание" },
        { "Active" }, // имя значения, а не description
    };

    /// <summary>
    /// Проверяет успешное получение значения перечисления по Description.
    /// </summary>
    /// <param name="description">Описание для поиска.</param>
    /// <param name="expected">Ожидаемое значение перечисления.</param>
    [Theory]
    [MemberData(nameof(ValidDescriptionCases))]
    public void GetEnumByDescription_ValidDescription_ReturnsCorrectEnumValue(
        string description,
        TestEnumWithDescriptions expected)
    {
        // Act
        var result = EnumHelper.GetEnumByDescription<TestEnumWithDescriptions>(description);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Проверяет, что поиск по Description регистронезависимый.
    /// </summary>
    [Fact]
    public void GetEnumByDescription_DescriptionComparison_IsCaseInsensitive()
    {
        // Arrange
        const string lowerCaseDescription = "активный статус";
        const string upperCaseDescription = "АКТИВНЫЙ СТАТУС";
        const string mixedCaseDescription = "Активный Статус";
        var expected = TestEnumWithDescriptions.Active;

        // Act
        var lowerCaseResult = EnumHelper.GetEnumByDescription<TestEnumWithDescriptions>(lowerCaseDescription);
        var upperCaseResult = EnumHelper.GetEnumByDescription<TestEnumWithDescriptions>(upperCaseDescription);
        var mixedCaseResult = EnumHelper.GetEnumByDescription<TestEnumWithDescriptions>(mixedCaseDescription);

        // Assert
        Assert.Equal(expected, lowerCaseResult);
        Assert.Equal(expected, upperCaseResult);
        Assert.Equal(expected, mixedCaseResult);
    }

    /// <summary>
    /// Проверяет выброс исключения при отсутствии найденного Description без emptyValue.
    /// </summary>
    [Theory]
    [MemberData(nameof(InvalidDescriptionCases))]
    public void GetEnumByDescription_InvalidDescription_ThrowsArgumentException(string? description)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            EnumHelper.GetEnumByDescription<TestEnumWithDescriptions>(description!));
    }

    /// <summary>
    /// Проверяет возврат emptyValue при отсутствии найденного Description.
    /// </summary>
    [Fact]
    public void GetEnumByDescription_InvalidDescriptionWithEmptyValue_ReturnsEmptyValue()
    {
        // Arrange
        const string invalidDescription = "Несуществующее описание";
        const TestEnumWithDescriptions emptyValue = TestEnumWithDescriptions.WithoutDescription;

        // Act
        var result = EnumHelper.GetEnumByDescription(invalidDescription, emptyValue);

        // Assert
        Assert.Equal(emptyValue, result);
    }

    /// <summary>
    /// Проверяет возврат emptyValue при пустом описании.
    /// </summary>
    [Fact]
    public void GetEnumByDescription_EmptyDescriptionWithEmptyValue_ReturnsEmptyValue()
    {
        // Arrange
        const string emptyDescription = "";
        const TestEnumWithDescriptions emptyValue = TestEnumWithDescriptions.Inactive;

        // Act
        var result = EnumHelper.GetEnumByDescription(emptyDescription, emptyValue);

        // Assert
        Assert.Equal(emptyValue, result);
    }

    /// <summary>
    /// Проверяет выброс исключения при пустом описании без emptyValue.
    /// </summary>
    [Fact]
    public void GetEnumByDescription_EmptyDescription_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            EnumHelper.GetEnumByDescription<TestEnumWithDescriptions>(""));
    }

    /// <summary>
    /// Проверяет работу с перечислением без Description-атрибутов.
    /// Должно возвращать emptyValue или бросать исключение.
    /// </summary>
    [Fact]
    public void GetEnumByDescription_EnumWithoutDescriptions_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            EnumHelper.GetEnumByDescription<TestEnumWithoutDescriptions>("Value1"));
    }

    /// <summary>
    /// Проверяет работу с null-описанием и предоставленным emptyValue.
    /// </summary>
    [Fact]
    public void GetEnumByDescription_NullDescriptionWithEmptyValue_ReturnsEmptyValue()
    {
        // Arrange
        const TestEnumWithDescriptions emptyValue = TestEnumWithDescriptions.Pending;

        // Act
        var result = EnumHelper.GetEnumByDescription<TestEnumWithDescriptions>(null, emptyValue);

        // Assert
        Assert.Equal(emptyValue, result);
    }
}
