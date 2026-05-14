// ----------------------------------------------------------------------------------------------
// <copyright file="EnumExtensionsTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Extensions;
using System.ComponentModel;

namespace Shared.Common.Tests.Helpers;

/// <summary>
/// Тесты для вспомогательного класса <see cref="EnumExtensions"/>.
/// Проверяет корректность получения значений перечислений по атрибуту Description.
/// </summary>
public sealed class EnumExtensionsTests
{
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
    public static TheoryData<string?> InvalidDescriptionCases { get; } =
    [
        (string?)null,
        "",
        "   ",
        "Несуществующее описание",
        "Active" // имя значения, а не description
    ];

    /// <summary>
    /// Тестовое перечисление с Description-атрибутами для проверки.
    /// </summary>
    public enum TestEnumWithDescriptions
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
    /// Тестовое перечисление с флагами для проверки <see cref="EnumExtensions.Without"/>.
    /// </summary>
    [Flags]
    private enum TestFlags
    {
        None = 0,
        FlagA = 1,
        FlagB = 2,
        FlagC = 4,
    }

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
        var result = EnumExtensions.GetEnumValueByDescription<TestEnumWithDescriptions>(description);

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
        var lowerCaseResult = EnumExtensions.GetEnumValueByDescription<TestEnumWithDescriptions>(lowerCaseDescription);
        var upperCaseResult = EnumExtensions.GetEnumValueByDescription<TestEnumWithDescriptions>(upperCaseDescription);
        var mixedCaseResult = EnumExtensions.GetEnumValueByDescription<TestEnumWithDescriptions>(mixedCaseDescription);

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
            EnumExtensions.GetEnumValueByDescription<TestEnumWithDescriptions>(description!));
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
        var result = EnumExtensions.GetEnumValueByDescription<TestEnumWithDescriptions>(invalidDescription, emptyValue);

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
        var result = EnumExtensions.GetEnumValueByDescription<TestEnumWithDescriptions>(emptyDescription, emptyValue);

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
            EnumExtensions.GetEnumValueByDescription<TestEnumWithDescriptions>(""));
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
            EnumExtensions.GetEnumValueByDescription<TestEnumWithoutDescriptions>("Value1"));
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
        var result = EnumExtensions.GetEnumValueByDescription<TestEnumWithDescriptions>(null, emptyValue);

        // Assert
        Assert.Equal(emptyValue, result);
    }

    /// <summary>
    /// Проверяет, что <see cref="EnumExtensions.Description"/> возвращает значение атрибута,
    /// когда атрибут <see cref="System.ComponentModel.DescriptionAttribute"/> задан.
    /// </summary>
    [Theory]
    [InlineData(TestEnumWithDescriptions.Active, "Активный статус")]
    [InlineData(TestEnumWithDescriptions.Inactive, "Неактивный статус")]
    [InlineData(TestEnumWithDescriptions.Pending, "Ожидание подтверждения")]
    public void Description_WithAttribute_ReturnsAttributeValue(
        TestEnumWithDescriptions value,
        string expected)
    {
        // Act
        var result = value.Description();

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Проверяет, что <see cref="EnumExtensions.Description"/> возвращает <see cref="string.Empty"/>,
    /// когда атрибут <see cref="System.ComponentModel.DescriptionAttribute"/> отсутствует.
    /// </summary>
    [Fact]
    public void Description_WithoutAttribute_ReturnsEmptyString()
    {
        // Act
        var result = TestEnumWithDescriptions.WithoutDescription.Description();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    /// Проверяет поиск по частичному совпадению (contextSearch = true).
    /// </summary>
    [Theory]
    [InlineData("активный", TestEnumWithDescriptions.Active)]
    [InlineData("подтверждения", TestEnumWithDescriptions.Pending)]
    [InlineData("НЕАКТИВНЫЙ", TestEnumWithDescriptions.Inactive)]
    public void GetEnumValueByDescription_ContextSearch_ReturnsPartialMatch(
        string partialDescription,
        TestEnumWithDescriptions expected)
    {
        // Act
        var result = partialDescription.GetEnumValueByDescription<TestEnumWithDescriptions>(contextSearch: true);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Проверяет, что при contextSearch = true и ненайденном значении без emptyValue бросается исключение.
    /// </summary>
    [Fact]
    public void GetEnumValueByDescription_ContextSearch_NoMatch_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            "несуществующий фрагмент".GetEnumValueByDescription<TestEnumWithDescriptions>(contextSearch: true));
    }

    /// <summary>
    /// Проверяет, что при contextSearch = true и ненайденном значении с emptyValue возвращается emptyValue.
    /// </summary>
    [Fact]
    public void GetEnumValueByDescription_ContextSearch_NoMatchWithEmptyValue_ReturnsEmptyValue()
    {
        // Arrange
        const TestEnumWithDescriptions emptyValue = TestEnumWithDescriptions.WithoutDescription;

        // Act
        var result = "несуществующий фрагмент".GetEnumValueByDescription<TestEnumWithDescriptions>(
            emptyValue: emptyValue,
            contextSearch: true);

        // Assert
        Assert.Equal(emptyValue, result);
    }

    /// <summary>
    /// Проверяет нежанровый overload <see cref="EnumExtensions.GetEnumValueByDescription(string, Type, Enum, bool)"/>:
    /// корректный поиск по описанию.
    /// </summary>
    [Fact]
    public void GetEnumValueByDescription_NonGenericOverload_ReturnsCorrectValue()
    {
        // Arrange
        const string description = "Активный статус";

        // Act
        var result = description.GetEnumValueByDescription(typeof(TestEnumWithDescriptions));

        // Assert
        Assert.Equal(TestEnumWithDescriptions.Active, result);
    }

    /// <summary>
    /// Проверяет нежанровый overload: возврат emptyValue при ненайденном описании.
    /// </summary>
    [Fact]
    public void GetEnumValueByDescription_NonGenericOverload_WithEmptyValue_ReturnsEmptyValue()
    {
        // Arrange
        Enum emptyValue = TestEnumWithDescriptions.WithoutDescription;

        // Act
        var result = "Несуществующее".GetEnumValueByDescription(typeof(TestEnumWithDescriptions), emptyValue);

        // Assert
        Assert.Equal(TestEnumWithDescriptions.WithoutDescription, result);
    }

    /// <summary>
    /// Проверяет нежанровый overload: исключение при ненайденном описании без emptyValue.
    /// </summary>
    [Fact]
    public void GetEnumValueByDescription_NonGenericOverload_InvalidDescription_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            "Несуществующее".GetEnumValueByDescription(typeof(TestEnumWithDescriptions)));
    }

    /// <summary>
    /// Проверяет, что <see cref="EnumExtensions.Without"/> корректно удаляет флаг (AND NOT, не XOR).
    /// </summary>
    [Fact]
    public void Without_FlagPresent_RemovesFlag()
    {
        // Arrange
        Enum flags = TestFlags.FlagA | TestFlags.FlagB;
        Enum flagToRemove = TestFlags.FlagA;

        // Act
        var result = flags.Without(flagToRemove);

        // Assert
        Assert.Equal(TestFlags.FlagB, result);
    }

    /// <summary>
    /// Проверяет, что <see cref="EnumExtensions.Without"/> не добавляет флаг, которого нет (AND NOT, не XOR).
    /// XOR-реализация при этом добавила бы флаг.
    /// </summary>
    [Fact]
    public void Without_FlagNotPresent_DoesNotAddFlag()
    {
        // Arrange
        Enum flags = TestFlags.FlagA;
        Enum flagToRemove = TestFlags.FlagB; // FlagB изначально не установлен

        // Act
        var result = flags.Without(flagToRemove);

        // Assert: AND NOT оставит FlagA неизменным; XOR бы добавил FlagB
        Assert.Equal(TestFlags.FlagA, result);
    }
}
