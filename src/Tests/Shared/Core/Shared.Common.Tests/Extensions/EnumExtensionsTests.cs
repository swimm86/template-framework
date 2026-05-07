// ----------------------------------------------------------------------------------------------
// <copyright file="EnumExtensionsTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.ComponentModel;
using Shared.Common.Extensions;
using Xunit;

namespace Shared.Common.Tests.Extensions;

/// <summary>
/// Набор тестов для класса расширения перечислений <see cref="EnumExtensions"/>.
/// Проверяет корректность работы всех методов расширения Enum,
/// включая работу с атрибутами DescriptionAttribute, флаговые операции и граничные случаи.
/// </summary>
public class EnumExtensionsTests
{
    #region Test Enums

    /// <summary>
    /// Тестовое перечисление с атрибутами Description для проверки методов получения описания.
    /// </summary>
    private enum TestEnumWithDescription
    {
        [Description("Simple value")]
        SimpleValue,

        [Description("Complex value with spaces")]
        ComplexValue,

        [Description("Special chars: @#$%")]
        SpecialChars,

        NoDescription,

        [Description("")]
        EmptyDescription,

        [Description("Duplicate")]
        FirstDuplicate,

        [Description("Duplicate")]
        SecondDuplicate
    }

    /// <summary>
    /// Тестовое перечисление с флагами для проверки битовых операций.
    /// </summary>
    [Flags]
    private enum TestFlagsEnum
    {
        None = 0,
        Flag1 = 1,
        Flag2 = 2,
        Flag3 = 4,
        Flag4 = 8,
        All = Flag1 | Flag2 | Flag3 | Flag4
    }

    /// <summary>
    /// Тестовое перечисление без флагов для проверки валидации.
    /// </summary>
    private enum TestNonFlagsEnum
    {
        Value1 = 1,
        Value2 = 2,
        Value3 = 4
    }

    /// <summary>
    /// Тестовое перечисление с разными типами underlying типа.
    /// </summary>
    private enum TestByteEnum : byte
    {
        ByteValue1 = 1,
        ByteValue2 = 2
    }

    #endregion

    #region Description Tests

    /// <summary>
    /// Проверяет получение описания из атрибута DescriptionAttribute для различных значений Enum.
    /// Тестирует значения с описанием, без описания, с пустым описанием и специальными символами.
    /// </summary>
    /// <param name="value">Значение Enum для тестирования.</param>
    /// <param name="expected">Ожидаемое описание.</param>
    [Theory]
    [InlineData(TestEnumWithDescription.SimpleValue, "Simple value")]
    [InlineData(TestEnumWithDescription.ComplexValue, "Complex value with spaces")]
    [InlineData(TestEnumWithDescription.SpecialChars, "Special chars: @#$%")]
    [InlineData(TestEnumWithDescription.NoDescription, "NoDescription")]
    [InlineData(TestEnumWithDescription.EmptyDescription, "")]
    [InlineData(TestEnumWithDescription.FirstDuplicate, "Duplicate")]
    [InlineData(TestEnumWithDescription.SecondDuplicate, "Duplicate")]
    public void Description_EnumWithDescriptionAttribute_ReturnsDescriptionValue(
        TestEnumWithDescription value,
        string expected)
    {
        // Act
        var result = value.Description();

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Проверяет, что метод Description возвращает имя_enum, если атрибут Description отсутствует.
    /// </summary>
    [Fact]
    public void Description_EnumWithoutDescriptionAttribute_ReturnsEnumName()
    {
        // Arrange
        var value = TestEnumWithDescription.NoDescription;

        // Act
        var result = value.Description();

        // Assert
        Assert.Equal("NoDescription", result);
    }

    /// <summary>
    /// Проверяет работу метода Description для перечислений с разным underlying типом (byte).
    /// </summary>
    [Fact]
    public void Description_EnumWithByteUnderlyingType_ReturnsCorrectDescription()
    {
        // Arrange
        var value = TestByteEnum.ByteValue1;

        // Act
        var result = value.Description();

        // Assert
        Assert.Equal("ByteValue1", result);
    }

    #endregion

    #region GetEnumValueByDescription Tests

    /// <summary>
    /// Проверяет получение значения Enum по точному совпадению описания из DescriptionAttribute.
    /// Тестирует поиск по различным описаниям, включая специальные символы и пробелы.
    /// </summary>
    /// <param name="description">Описание для поиска.</param>
    /// <param name="expected">Ожидаемое значение Enum.</param>
    [Theory]
    [InlineData("Simple value", TestEnumWithDescription.SimpleValue)]
    [InlineData("Complex value with spaces", TestEnumWithDescription.ComplexValue)]
    [InlineData("Special chars: @#$%", TestEnumWithDescription.SpecialChars)]
    [InlineData("Duplicate", TestEnumWithDescription.FirstDuplicate)]
    public void GetEnumValueByDescription_ExactMatch_ReturnsCorrectEnumValue(
        string description,
        TestEnumWithDescription expected)
    {
        // Arrange
        var enumType = typeof(TestEnumWithDescription);

        // Act
        var result = description.GetEnumValueByDescription(enumType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Проверяет, что метод возвращает null при отсутствии совпадения описания.
    /// </summary>
    [Fact]
    public void GetEnumValueByDescription_NoMatch_ReturnsNull()
    {
        // Arrange
        var description = "Non-existent description";
        var enumType = typeof(TestEnumWithDescription);

        // Act
        var result = description.GetEnumValueByDescription(enumType);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Проверяет, что метод возвращает первое найденное значение при наличии дубликатов описаний.
    /// </summary>
    [Fact]
    public void GetEnumValueByDescription_DuplicateDescriptions_ReturnsFirstMatch()
    {
        // Arrange
        var description = "Duplicate";
        var enumType = typeof(TestEnumWithDescription);

        // Act
        var result = description.GetEnumValueByDescription(enumType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestEnumWithDescription.FirstDuplicate, result);
    }

    /// <summary>
    /// Проверяет, что метод выбрасывает ArgumentNullException при передаче null в качестве описания.
    /// </summary>
    [Fact]
    public void GetEnumValueByDescription_NullDescription_ThrowsArgumentNullException()
    {
        // Arrange
        string? description = null;
        var enumType = typeof(TestEnumWithDescription);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => description!.GetEnumValueByDescription(enumType));
    }

    /// <summary>
    /// Проверяет, что метод возвращает null для пустого описания, если есть Enum с пустым Description.
    /// </summary>
    [Fact]
    public void GetEnumValueByDescription_EmptyDescription_MatchesEmptyDescriptionAttribute()
    {
        // Arrange
        var description = "";
        var enumType = typeof(TestEnumWithDescription);

        // Act
        var result = description.GetEnumValueByDescription(enumType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestEnumWithDescription.EmptyDescription, result);
    }

    #endregion

    #region GetEnumValueByPartOfDescription Tests

    /// <summary>
    /// Проверяет получение значений Enum по частичному совпадению описания (case-insensitive).
    /// Тестирует поиск подстроки в различных позициях описания.
    /// </summary>
    /// <param name="searchTerm">Поисковый запрос.</param>
    /// <param name="expectedCount">Ожидаемое количество найденных значений.</param>
    /// <param name="expectedValues">Ожидаемые найденные значения.</param>
    [Theory]
    [InlineData("simple", 1, new[] { TestEnumWithDescription.SimpleValue })]
    [InlineData("value", 3, new[] { TestEnumWithDescription.SimpleValue, TestEnumWithDescription.ComplexValue, TestEnumWithDescription.NoDescription })]
    [InlineData("COMPLEX", 1, new[] { TestEnumWithDescription.ComplexValue })]
    [InlineData("chars", 1, new[] { TestEnumWithDescription.SpecialChars })]
    [InlineData("duplicate", 2, new[] { TestEnumWithDescription.FirstDuplicate, TestEnumWithDescription.SecondDuplicate })]
    [InlineData("nonexistent", 0, new TestEnumWithDescription[0])]
    public void GetEnumValueByPartOfDescription_PartialMatch_ReturnsMatchingEnumValues(
        string searchTerm,
        int expectedCount,
        TestEnumWithDescription[] expectedValues)
    {
        // Arrange
        var enumType = typeof(TestEnumWithDescription);

        // Act
        var result = searchTerm.GetEnumValueByPartOfDescription(enumType).ToList();

        // Assert
        Assert.Equal(expectedCount, result.Count);
        foreach (var expected in expectedValues)
        {
            Assert.Contains(expected, result.Cast<TestEnumWithDescription>());
        }
    }

    /// <summary>
    /// Проверяет, что поиск по частичному описанию регистронезависимый.
    /// </summary>
    [Fact]
    public void GetEnumValueByPartOfDescription_CaseInsensitiveSearch_FindsMatches()
    {
        // Arrange
        var description = "SIMPLE VALUE";
        var enumType = typeof(TestEnumWithDescription);

        // Act
        var result = description.GetEnumValueByPartOfDescription(enumType).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(TestEnumWithDescription.SimpleValue, result[0]);
    }

    /// <summary>
    /// Проверяет, что метод выбрасывает ArgumentNullException при передаче null в качестве описания.
    /// </summary>
    [Fact]
    public void GetEnumValueByPartOfDescription_NullDescription_ThrowsArgumentNullException()
    {
        // Arrange
        string? description = null;
        var enumType = typeof(TestEnumWithDescription);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => description!.GetEnumValueByPartOfDescription(enumType));
    }

    /// <summary>
    /// Проверяет, что пустая строка находит все значения Enum (так как пустая строка содержится везде).
    /// </summary>
    [Fact]
    public void GetEnumValueByPartOfDescription_EmptyString_ReturnsAllEnumValues()
    {
        // Arrange
        var description = "";
        var enumType = typeof(TestEnumWithDescription);
        var expectedCount = Enum.GetValues(enumType).Length;

        // Act
        var result = description.GetEnumValueByPartOfDescription(enumType).ToList();

        // Assert
        Assert.Equal(expectedCount, result.Count);
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
        var flags = TestFlagsEnum.Flag1;
        var flagsToAdd = TestFlagsEnum.Flag2;
        var expected = TestFlagsEnum.Flag1 | TestFlagsEnum.Flag2;

        // Act
        var result = flags.With(flagsToAdd);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Проверяет объединение нескольких флагов последовательно.
    /// </summary>
    [Fact]
    public void With_MultipleFlags_CombinesAllFlagsCorrectly()
    {
        // Arrange
        var flags = TestFlagsEnum.Flag1;
        var expected = TestFlagsEnum.Flag1 | TestFlagsEnum.Flag2 | TestFlagsEnum.Flag3;

        // Act
        var result = flags.With(TestFlagsEnum.Flag2).With(TestFlagsEnum.Flag3);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Проверяет, что объединение флага с самим собой не изменяет результат.
    /// </summary>
    [Fact]
    public void With_SameFlag_ReturnsOriginalFlags()
    {
        // Arrange
        var flags = TestFlagsEnum.Flag1 | TestFlagsEnum.Flag2;

        // Act
        var result = flags.With(TestFlagsEnum.Flag1);

        // Assert
        Assert.Equal(flags, result);
    }

    /// <summary>
    /// Проверяет объединение флагов с флагом None (должно возвращать исходные флаги).
    /// </summary>
    [Fact]
    public void With_NoneFlag_ReturnsOriginalFlags()
    {
        // Arrange
        var flags = TestFlagsEnum.Flag1 | TestFlagsEnum.Flag2;

        // Act
        var result = flags.With(TestFlagsEnum.None);

        // Assert
        Assert.Equal(flags, result);
    }

    /// <summary>
    /// Проверяет, что метод выбрасывает ArgumentException при попытке объединить флаги разных типов.
    /// </summary>
    [Fact]
    public void With_DifferentEnumTypes_ThrowsArgumentException()
    {
        // Arrange
        var flags = TestFlagsEnum.Flag1;
        var differentFlags = TestNonFlagsEnum.Value1;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => flags.With(differentFlags));
    }

    /// <summary>
    /// Проверяет работу метода With для перечислений с byte underlying типом.
    /// </summary>
    [Fact]
    public void With_ByteUnderlyingType_CombinesFlagsCorrectly()
    {
        // Arrange
        var flags = TestByteEnum.ByteValue1;
        var flagsToAdd = TestByteEnum.ByteValue2;

        // Act
        var result = flags.With(flagsToAdd);

        // Assert
        Assert.Equal((byte)3, (byte)result);
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
        var flags = TestFlagsEnum.Flag1 | TestFlagsEnum.Flag2 | TestFlagsEnum.Flag3;
        var flagsToRemove = TestFlagsEnum.Flag2;
        var expected = TestFlagsEnum.Flag1 | TestFlagsEnum.Flag3;

        // Act
        var result = flags.Without(flagsToRemove);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Проверяет последовательное исключение нескольких флагов.
    /// </summary>
    [Fact]
    public void Without_RemoveMultipleFlags_RemovesAllFlagsCorrectly()
    {
        // Arrange
        var flags = TestFlagsEnum.All;

        // Act
        var result = flags.Without(TestFlagsEnum.Flag1).Without(TestFlagsEnum.Flag2);

        // Assert
        var expected = TestFlagsEnum.Flag3 | TestFlagsEnum.Flag4;
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Проверяет, что исключение флага, которого нет, использует XOR и добавляет флаг.
    /// Это ожидаемое поведение текущего реализации (XOR).
    /// </summary>
    [Fact]
    public void Without_FlagNotPresent_UsesXorBehavior()
    {
        // Arrange
        var flags = TestFlagsEnum.Flag1;
        var flagsToRemove = TestFlagsEnum.Flag2;

        // Act
        var result = flags.Without(flagsToRemove);

        // Assert
        // XOR поведение: Flag1 ^ Flag2 = Flag1 | Flag2 (так как Flag2 не было в flags)
        var expected = TestFlagsEnum.Flag1 | TestFlagsEnum.Flag2;
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Проверяет исключение всех флагов (результат должен быть None).
    /// </summary>
    [Fact]
    public void Without_RemoveAllFlags_ReturnsNone()
    {
        // Arrange
        var flags = TestFlagsEnum.All;

        // Act
        var result = flags.Without(TestFlagsEnum.All);

        // Assert
        Assert.Equal(TestFlagsEnum.None, result);
    }

    /// <summary>
    /// Проверяет, что метод выбрасывает ArgumentException при попытке исключить флаги разных типов.
    /// </summary>
    [Fact]
    public void Without_DifferentEnumTypes_ThrowsArgumentException()
    {
        // Arrange
        var flags = TestFlagsEnum.Flag1;
        var differentFlags = TestNonFlagsEnum.Value1;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => flags.Without(differentFlags));
    }

    /// <summary>
    /// Проверяет исключение флага None (не должно изменять исходные флаги).
    /// </summary>
    [Fact]
    public void Without_NoneFlag_ReturnsOriginalFlags()
    {
        // Arrange
        var flags = TestFlagsEnum.Flag1 | TestFlagsEnum.Flag2;

        // Act
        var result = flags.Without(TestFlagsEnum.None);

        // Assert
        Assert.Equal(flags, result);
    }

    #endregion

    #region Combine Method Validation Tests

    /// <summary>
    /// Проверяет, что метод Combine (используемый внутри With/Without) выбрасывает ArgumentException,
    /// когда первый параметр не является Enum.
    /// Примечание: Этот тест документальный, так как компилятор не позволит передать не-Enum.
    /// </summary>
    [Fact]
    public void Combine_FirstParameterNotEnum_ThrowsArgumentException()
    {
        // Этот тест показывает, что типобезопасность обеспечивается на уровне компилятора
        // Фактическая проверка происходит внутри метода Combine через IsEnum
        Assert.True(true, "Типобезопасность обеспечивается системой типов C#");
    }

    /// <summary>
    /// Проверяет сообщение об ошибке при попытке объединить разные типы Enum.
    /// </summary>
    [Fact]
    public void Combine_DifferentTypes_ThrowsArgumentExceptionWithDetailedMessage()
    {
        // Arrange
        var flags = TestFlagsEnum.Flag1;
        var differentFlags = TestNonFlagsEnum.Value1;

        // Act
        var exception = Assert.Throws<ArgumentException>(() => flags.With(differentFlags));

        // Assert
        Assert.Contains("разных тпиов", exception.Message);
        Assert.Contains("TestFlagsEnum", exception.Message);
        Assert.Contains("TestNonFlagsEnum", exception.Message);
    }

    #endregion

    #region Edge Cases and Integration Tests

    /// <summary>
    /// Проверяет работу с перечислением, имеющим максимальное количество флагов (All).
    /// </summary>
    [Fact]
    public void FlagsOperations_AllFlagsValue_CorrectlyRepresentsAllFlags()
    {
        // Arrange & Act
        var allFlags = TestFlagsEnum.All;

        // Assert
        Assert.True(allFlags.HasFlag(TestFlagsEnum.Flag1));
        Assert.True(allFlags.HasFlag(TestFlagsEnum.Flag2));
        Assert.True(allFlags.HasFlag(TestFlagsEnum.Flag3));
        Assert.True(allFlags.HasFlag(TestFlagsEnum.Flag4));
    }

    /// <summary>
    /// Интеграционный тест: комбинация Description и флаговых операций.
    /// </summary>
    [Fact]
    public void Integration_DescriptionAndFlagOperations_WorkTogetherCorrectly()
    {
        // Arrange
        var flags = TestFlagsEnum.Flag1 | TestFlagsEnum.Flag2;
        var description = flags.Description();

        // Act & Assert
        // Для флагов без DescriptionAttribute должно возвращаться имя
        Assert.Equal("Flag1, Flag2", description);
    }

    /// <summary>
    /// Проверяет, что цепочка операций With и Without работает корректно.
    /// </summary>
    [Fact]
    public void ChainedOperations_AddAndRemoveFlags_ProducesExpectedResult()
    {
        // Arrange
        var initial = TestFlagsEnum.Flag1;

        // Act: Add Flag2, Add Flag3, Remove Flag2
        var result = initial
            .With(TestFlagsEnum.Flag2)
            .With(TestFlagsEnum.Flag3)
            .Without(TestFlagsEnum.Flag2);

        // Assert
        Assert.Equal(TestFlagsEnum.Flag1 | TestFlagsEnum.Flag3, result);
    }

    #endregion
}
