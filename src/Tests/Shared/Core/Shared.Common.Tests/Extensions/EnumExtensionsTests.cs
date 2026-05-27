// ----------------------------------------------------------------------------------------------
// <copyright file="EnumExtensionsTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Extensions;
using System.ComponentModel;

namespace Shared.Common.Tests.Extensions;

/// <summary>
/// Тесты для вспомогательного класса <see cref="EnumExtensions"/>.
/// Проверяет корректность получения значений перечислений по атрибуту Description,
/// флаговые операции и граничные случаи.
/// </summary>
public sealed class EnumExtensionsTests
{
    #region Test Enums

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

        WithoutDescription,

        [Description("")]
        EmptyDescription,

        [Description("Duplicate")]
        FirstDuplicate,

        [Description("Duplicate")]
        SecondDuplicate,

        [Description("Special chars: @#$%")]
        SpecialChars,
    }

    /// <summary>
    /// Тестовое перечисление без Description-атрибутов.
    /// </summary>
    private enum TestEnumWithoutDescriptions
    {
        Value1,
        Value2,
        Value3,
    }

    /// <summary>
    /// Тестовое перечисление с флагами для проверки <see cref="EnumExtensions.With"/> и <see cref="EnumExtensions.Without"/>.
    /// </summary>
    [Flags]
    private enum TestFlags
    {
        None = 0,
        FlagA = 1,
        FlagB = 2,
        FlagC = 4,
        FlagD = 8,
        All = FlagA | FlagB | FlagC | FlagD,
    }

    /// <summary>
    /// Тестовое перечисление без флагов для проверки валидации типов.
    /// </summary>
    private enum TestNonFlagsEnum
    {
        Value1 = 1,
        Value2 = 2,
        Value3 = 4,
    }

    /// <summary>
    /// Тестовое перечисление с byte underlying типом.
    /// </summary>
    private enum TestByteEnum : byte
    {
        ByteValue1 = 1,
        ByteValue2 = 2,
    }

    #endregion

    #region Description Tests

    /// <summary>
    /// Проверяет, что <see cref="EnumExtensions.Description"/> возвращает значение атрибута,
    /// когда атрибут <see cref="DescriptionAttribute"/> задан.
    /// </summary>
    [Theory]
    [InlineData(TestEnumWithDescriptions.Active, "Активный статус")]
    [InlineData(TestEnumWithDescriptions.Inactive, "Неактивный статус")]
    [InlineData(TestEnumWithDescriptions.Pending, "Ожидание подтверждения")]
    [InlineData(TestEnumWithDescriptions.SpecialChars, "Special chars: @#$%")]
    [InlineData(TestEnumWithDescriptions.FirstDuplicate, "Duplicate")]
    [InlineData(TestEnumWithDescriptions.SecondDuplicate, "Duplicate")]
    public void Description_WithAttribute_ReturnsAttributeValue(
        TestEnumWithDescriptions value,
        string expected)
    {
        // Act
        var result = value.Description();

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Проверяет, что <see cref="EnumExtensions.Description"/> возвращает <see cref="string.Empty"/>,
    /// когда атрибут <see cref="DescriptionAttribute"/> отсутствует.
    /// </summary>
    [Fact]
    public void Description_WithoutAttribute_ReturnsEmptyString()
    {
        // Act
        var result = TestEnumWithDescriptions.WithoutDescription.Description();

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет, что <see cref="EnumExtensions.Description"/> возвращает пустую строку
    /// для значения с пустым DescriptionAttribute.
    /// </summary>
    [Fact]
    public void Description_WithEmptyAttribute_ReturnsEmptyString()
    {
        // Act
        var result = TestEnumWithDescriptions.EmptyDescription.Description();

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет работу метода Description для перечислений с byte underlying типом.
    /// </summary>
    [Fact]
    public void Description_EnumWithByteUnderlyingType_ReturnsCorrectDescription()
    {
        // Act
        var result = TestByteEnum.ByteValue1.Description();

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет, что <see cref="EnumExtensions.Description"/> возвращает <see cref="string.Empty"/>
    /// для комбинации флагов (у combined flags нет соответствующего поля в enum).
    /// </summary>
    [Fact]
    public void Description_CombinedFlags_ReturnsEmptyString()
    {
        // Arrange
        var flags = TestFlags.FlagA | TestFlags.FlagB;

        // Act
        var result = flags.Description();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetEnumValueByDescription Tests (Generic)

    /// <summary>
    /// Тестовые данные для успешного поиска по Description.
    /// </summary>
    public static TheoryData<string, TestEnumWithDescriptions> ValidDescriptionCases { get; } = new()
    {
        { "Активный статус", TestEnumWithDescriptions.Active },
        { "активный статус", TestEnumWithDescriptions.Active },
        { "АКТИВНЫЙ СТАТУС", TestEnumWithDescriptions.Active },
        { "Неактивный статус", TestEnumWithDescriptions.Inactive },
        { "Ожидание подтверждения", TestEnumWithDescriptions.Pending },
        { "Special chars: @#$%", TestEnumWithDescriptions.SpecialChars },
    };

    /// <summary>
    /// Проверяет успешное получение значения перечисления по Description.
    /// </summary>
    [Theory]
    [MemberData(nameof(ValidDescriptionCases))]
    public void GetEnumValueByDescription_ValidDescription_ReturnsCorrectEnumValue(
        string description,
        TestEnumWithDescriptions expected)
    {
        // Act
        var result = description.GetEnumValueByDescription<TestEnumWithDescriptions>();

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Проверяет, что поиск по Description регистронезависимый.
    /// </summary>
    /// <param name="description">Описание в произвольном регистре.</param>
    [Theory]
    [InlineData("активный статус")]
    [InlineData("АКТИВНЫЙ СТАТУС")]
    [InlineData("Активный Статус")]
    public void GetEnumValueByDescription_DescriptionComparison_IsCaseInsensitive(string description)
    {
        // Arrange
        var expected = TestEnumWithDescriptions.Active;

        // Act
        var result = description.GetEnumValueByDescription<TestEnumWithDescriptions>();

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Проверяет, что при наличии дубликатов Description возвращается первое найденное значение.
    /// </summary>
    [Fact]
    public void GetEnumValueByDescription_DuplicateDescriptions_ReturnsFirstMatch()
    {
        // Act
        var result = "Duplicate".GetEnumValueByDescription<TestEnumWithDescriptions>();

        // Assert
        result.Should().Be(TestEnumWithDescriptions.FirstDuplicate);
    }

    /// <summary>
    /// Проверяет выброс исключения при отсутствии найденного Description без emptyValue.
    /// </summary>
    public static TheoryData<string?> InvalidDescriptionCases { get; } =
    [
        (string?)null,
        "",
        "   ",
        "Несуществующее описание",
        "Active",
    ];

    [Theory]
    [MemberData(nameof(InvalidDescriptionCases))]
    public void GetEnumValueByDescription_InvalidDescription_ThrowsArgumentException(string? description)
    {
        // Act
        var act = () => description.GetEnumValueByDescription<TestEnumWithDescriptions>();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Проверяет возврат emptyValue при отсутствии найденного Description.
    /// </summary>
    [Fact]
    public void GetEnumValueByDescription_InvalidDescriptionWithEmptyValue_ReturnsEmptyValue()
    {
        // Arrange
        const string invalidDescription = "Несуществующее описание";
        const TestEnumWithDescriptions emptyValue = TestEnumWithDescriptions.WithoutDescription;

        // Act
        var result = invalidDescription.GetEnumValueByDescription<TestEnumWithDescriptions>(emptyValue);

        // Assert
        result.Should().Be(emptyValue);
    }

    /// <summary>
    /// Проверяет возврат emptyValue при пустом описании.
    /// </summary>
    [Fact]
    public void GetEnumValueByDescription_EmptyDescriptionWithEmptyValue_ReturnsEmptyValue()
    {
        // Arrange
        const string emptyDescription = "";
        const TestEnumWithDescriptions emptyValue = TestEnumWithDescriptions.Inactive;

        // Act
        var result = emptyDescription.GetEnumValueByDescription<TestEnumWithDescriptions>(emptyValue);

        // Assert
        result.Should().Be(emptyValue);
    }

    /// <summary>
    /// Проверяет выброс исключения при пустом описании без emptyValue.
    /// </summary>
    [Fact]
    public void GetEnumValueByDescription_EmptyDescription_ThrowsArgumentException()
    {
        // Act
        var act = () => "".GetEnumValueByDescription<TestEnumWithDescriptions>();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Проверяет работу с перечислением без Description-атрибутов.
    /// </summary>
    [Fact]
    public void GetEnumValueByDescription_EnumWithoutDescriptions_ThrowsArgumentException()
    {
        // Act
        var act = () => "Value1".GetEnumValueByDescription<TestEnumWithoutDescriptions>();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Проверяет работу с null-описанием и предоставленным emptyValue.
    /// </summary>
    [Fact]
    public void GetEnumValueByDescription_NullDescriptionWithEmptyValue_ReturnsEmptyValue()
    {
        // Arrange
        const TestEnumWithDescriptions emptyValue = TestEnumWithDescriptions.Pending;

        // Act
        var result = EnumExtensions.GetEnumValueByDescription<TestEnumWithDescriptions>(null, emptyValue);

        // Assert
        result.Should().Be(emptyValue);
    }

    #endregion

    #region GetEnumValueByDescription Tests (Non-Generic)

    /// <summary>
    /// Проверяет нежанровый overload <see cref="EnumExtensions.GetEnumValueByDescription(string?, Type, Enum?)"/>:
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
        result.Should().Be(TestEnumWithDescriptions.Active);
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
        result.Should().Be(TestEnumWithDescriptions.WithoutDescription);
    }

    /// <summary>
    /// Проверяет нежанровый overload: исключение при ненайденном описании без emptyValue.
    /// </summary>
    [Fact]
    public void GetEnumValueByDescription_NonGenericOverload_InvalidDescription_ThrowsArgumentException()
    {
        // Act
        var act = () => "Несуществующее".GetEnumValueByDescription(typeof(TestEnumWithDescriptions));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region GetEnumValueByPartOfDescription Tests

    /// <summary>
    /// Проверяет получение значений Enum по частичному совпадению описания (case-insensitive).
    /// </summary>
    [Theory]
    [InlineData("активный", 2, new[] { TestEnumWithDescriptions.Active, TestEnumWithDescriptions.Inactive })]
    [InlineData("статус", 2, new[] { TestEnumWithDescriptions.Active, TestEnumWithDescriptions.Inactive })]
    [InlineData("подтверждения", 1, new[] { TestEnumWithDescriptions.Pending })]
    [InlineData("duplicate", 2, new[] { TestEnumWithDescriptions.FirstDuplicate, TestEnumWithDescriptions.SecondDuplicate })]
    [InlineData("nonexistent", 0, new TestEnumWithDescriptions[0])]
    public void GetEnumValueByPartOfDescription_PartialMatch_ReturnsMatchingEnumValues(
        string searchTerm,
        int expectedCount,
        TestEnumWithDescriptions[] expectedValues)
    {
        // Arrange
        var enumType = typeof(TestEnumWithDescriptions);

        // Act
        var result = searchTerm.GetEnumValueByPartOfDescription(enumType).ToArray();

        // Assert
        result.Cast<TestEnumWithDescriptions>().Should()
            .HaveCount(expectedCount)
            .And.Equal(expectedValues);
    }

    /// <summary>
    /// Проверяет, что поиск по частичному описанию регистронезависимый.
    /// </summary>
    [Fact]
    public void GetEnumValueByPartOfDescription_CaseInsensitiveSearch_FindsMatches()
    {
        // Arrange
        var description = "ОЖИДАНИЕ";
        var enumType = typeof(TestEnumWithDescriptions);

        // Act
        var result = description.GetEnumValueByPartOfDescription(enumType).ToList();

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be(TestEnumWithDescriptions.Pending);
    }

    /// <summary>
    /// Проверяет, что метод выбрасывает ArgumentNullException при передаче null.
    /// </summary>
    [Fact]
    public void GetEnumValueByPartOfDescription_NullDescription_ThrowsArgumentNullException()
    {
        // Arrange
        string? description = null;
        var enumType = typeof(TestEnumWithDescriptions);

        // Act
        var act = () => description.GetEnumValueByPartOfDescription(enumType);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Проверяет, что пустая строка находит все значения Enum,
    /// так как каждая строка содержит пустую подстроку.
    /// </summary>
    [Fact]
    public void GetEnumValueByPartOfDescription_EmptyString_ReturnsAllEnumValues()
    {
        // Arrange
        var description = "";
        var enumType = typeof(TestEnumWithDescriptions);
        var expected = Enum.GetValues<TestEnumWithDescriptions>();

        // Act
        var result = description.GetEnumValueByPartOfDescription(enumType)
            .Cast<TestEnumWithDescriptions>()
            .ToArray();

        // Assert
        result.Should().Equal(expected);
    }

    #endregion

    #region With Tests (Flag Combination)

    /// <summary>
    /// Проверяет объединение двух флагов с помощью оператора OR.
    /// </summary>
    [Fact]
    public void With_TwoSingleFlags_CombinesFlagsCorrectly()
    {
        // Arrange
        Enum flags = TestFlags.FlagA;
        Enum flagsToAdd = TestFlags.FlagB;
        var expected = TestFlags.FlagA | TestFlags.FlagB;

        // Act
        var result = flags.With(flagsToAdd);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Проверяет объединение нескольких флагов последовательно.
    /// </summary>
    [Fact]
    public void With_MultipleFlags_CombinesAllFlagsCorrectly()
    {
        // Arrange
        Enum flags = TestFlags.FlagA;
        var expected = TestFlags.FlagA | TestFlags.FlagB | TestFlags.FlagC;

        // Act
        var result = flags.With(TestFlags.FlagB).With(TestFlags.FlagC);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Проверяет, что объединение флага с самим собой не изменяет результат.
    /// </summary>
    [Fact]
    public void With_SameFlag_ReturnsOriginalFlags()
    {
        // Arrange
        Enum flags = TestFlags.FlagA | TestFlags.FlagB;

        // Act
        var result = flags.With(TestFlags.FlagA);

        // Assert
        result.Should().Be(flags);
    }

    /// <summary>
    /// Проверяет объединение флагов с флагом None.
    /// </summary>
    [Fact]
    public void With_NoneFlag_ReturnsOriginalFlags()
    {
        // Arrange
        Enum flags = TestFlags.FlagA | TestFlags.FlagB;

        // Act
        var result = flags.With(TestFlags.None);

        // Assert
        result.Should().Be(flags);
    }

    /// <summary>
    /// Проверяет, что метод выбрасывает ArgumentException при попытке объединить флаги разных типов.
    /// </summary>
    [Fact]
    public void With_DifferentEnumTypes_ThrowsArgumentException()
    {
        // Arrange
        Enum flags = TestFlags.FlagA;
        Enum differentFlags = TestNonFlagsEnum.Value1;

        // Act
        var act = () => flags.With(differentFlags);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Проверяет работу метода With для перечислений с byte underlying типом.
    /// </summary>
    [Fact]
    public void With_ByteUnderlyingType_CombinesFlagsCorrectly()
    {
        // Arrange
        Enum flags = TestByteEnum.ByteValue1;
        Enum flagsToAdd = TestByteEnum.ByteValue2;

        // Act
        var result = flags.With(flagsToAdd);

        // Assert
        result.Should().Be((TestByteEnum)3);
    }

    #endregion

    #region Without Tests (Flag Exclusion)

    /// <summary>
    /// Проверяет исключение одного флага из комбинации флагов.
    /// </summary>
    [Fact]
    public void Without_RemoveSingleFlagFromCombination_RemovesFlagCorrectly()
    {
        // Arrange
        Enum flags = TestFlags.FlagA | TestFlags.FlagB | TestFlags.FlagC;
        Enum flagsToRemove = TestFlags.FlagB;
        var expected = TestFlags.FlagA | TestFlags.FlagC;

        // Act
        var result = flags.Without(flagsToRemove);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Проверяет последовательное исключение нескольких флагов.
    /// </summary>
    [Fact]
    public void Without_RemoveMultipleFlags_RemovesAllFlagsCorrectly()
    {
        // Arrange
        Enum flags = TestFlags.All;

        // Act
        var result = flags.Without(TestFlags.FlagA).Without(TestFlags.FlagB);

        // Assert
        var expected = TestFlags.FlagC | TestFlags.FlagD;
        result.Should().Be(expected);
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
        result.Should().Be(TestFlags.FlagB);
    }

    /// <summary>
    /// Проверяет, что <see cref="EnumExtensions.Without"/> не добавляет флаг, которого нет (AND NOT, не XOR).
    /// </summary>
    [Fact]
    public void Without_FlagNotPresent_DoesNotAddFlag()
    {
        // Arrange
        Enum flags = TestFlags.FlagA;
        Enum flagToRemove = TestFlags.FlagB;

        // Act
        var result = flags.Without(flagToRemove);

        // Assert: AND NOT оставит FlagA неизменным
        result.Should().Be(TestFlags.FlagA);
    }

    /// <summary>
    /// Проверяет исключение всех флагов (результат должен быть None).
    /// </summary>
    [Fact]
    public void Without_RemoveAllFlags_ReturnsNone()
    {
        // Arrange
        Enum flags = TestFlags.All;

        // Act
        var result = flags.Without(TestFlags.All);

        // Assert
        result.Should().Be(TestFlags.None);
    }

    /// <summary>
    /// Проверяет, что метод выбрасывает ArgumentException при попытке исключить флаги разных типов.
    /// </summary>
    [Fact]
    public void Without_DifferentEnumTypes_ThrowsArgumentException()
    {
        // Arrange
        Enum flags = TestFlags.FlagA;
        Enum differentFlags = TestNonFlagsEnum.Value1;

        // Act
        var act = () => flags.Without(differentFlags);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Проверяет исключение флага None (не должно изменять исходные флаги).
    /// </summary>
    [Fact]
    public void Without_NoneFlag_ReturnsOriginalFlags()
    {
        // Arrange
        Enum flags = TestFlags.FlagA | TestFlags.FlagB;

        // Act
        var result = flags.Without(TestFlags.None);

        // Assert
        result.Should().Be(flags);
    }

    #endregion

    #region Edge Cases and Integration Tests

    /// <summary>
    /// Проверяет, что All-флаг корректно представляет все флаги.
    /// </summary>
    /// <param name="flag">Проверяемый флаг.</param>
    [Theory]
    [InlineData(TestFlags.FlagA)]
    [InlineData(TestFlags.FlagB)]
    [InlineData(TestFlags.FlagC)]
    [InlineData(TestFlags.FlagD)]
    public void FlagsOperations_AllFlagsValue_CorrectlyRepresentsAllFlags(Enum flag)
    {
        // Act
        var allFlags = TestFlags.All;

        // Assert
        allFlags.HasFlag(flag).Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что цепочка операций With и Without работает корректно.
    /// </summary>
    [Fact]
    public void ChainedOperations_AddAndRemoveFlags_ProducesExpectedResult()
    {
        // Arrange
        Enum initial = TestFlags.FlagA;

        // Act: Add FlagB, Add FlagC, Remove FlagB
        var result = initial
            .With(TestFlags.FlagB)
            .With(TestFlags.FlagC)
            .Without(TestFlags.FlagB);

        // Assert
        result.Should().Be(TestFlags.FlagA | TestFlags.FlagC);
    }

    /// <summary>
    /// Проверяет сообщение об ошибке при попытке объединить разные типы Enum.
    /// </summary>
    [Fact]
    public void Combine_DifferentTypes_ThrowsArgumentExceptionWithDetailedMessage()
    {
        // Arrange
        Enum flags = TestFlags.FlagA;
        Enum differentFlags = TestNonFlagsEnum.Value1;

        // Act
        var act = () => flags.With(differentFlags);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*different types*TestFlags*TestNonFlagsEnum*");
    }

    #endregion
}
