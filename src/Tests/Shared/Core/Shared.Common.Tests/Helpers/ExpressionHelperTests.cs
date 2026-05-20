// ----------------------------------------------------------------------------------------------
// <copyright file="ExpressionHelperTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Helpers;

namespace Shared.Common.Tests.Helpers;

/// <summary>
/// Тесты для вспомогательного класса <see cref="ExpressionHelper"/>.
/// Проверяет корректность создания лямбда-выражений для доступа к свойствам объектов.
/// </summary>
public sealed class ExpressionHelperTests
{
    /// <summary>
    /// Тестовый класс с различными свойствами для проверки выражений.
    /// </summary>
    private class TestObject
    {
        public int IntProperty { get; set; }
        public string? StringProperty { get; set; }
        public DateTime DateTimeProperty { get; set; }
        public NestedObject? Nested { get; set; }
    }

    /// <summary>
    /// Вложенный тестовый класс для проверки цепочек свойств.
    /// </summary>
    private class NestedObject
    {
        public int Value { get; set; }
        public string? Text { get; set; }
    }

    /// <summary>
    /// Проверяет создание выражения для простого свойства типа int.
    /// </summary>
    [Fact]
    public void GetPropExpression_SimpleIntProperty_CreatesCorrectExpression()
    {
        // Arrange
        const string propertyName = "IntProperty";
        var testObject = new TestObject { IntProperty = 42 };

        // Act
        var expression = ExpressionHelper.GetPropExpression<TestObject>(propertyName);
        var compiledFunc = expression.Compile();
        var result = compiledFunc(testObject);

        // Assert
        result.Should().Be(42);
    }

    /// <summary>
    /// Проверяет создание выражения для простого свойства типа string.
    /// </summary>
    [Fact]
    public void GetPropExpression_SimpleStringProperty_CreatesCorrectExpression()
    {
        // Arrange
        const string propertyName = "StringProperty";
        var testObject = new TestObject { StringProperty = "TestValue" };

        // Act
        var expression = ExpressionHelper.GetPropExpression<TestObject>(propertyName);
        var compiledFunc = expression.Compile();
        var result = compiledFunc(testObject);

        // Assert
        result.Should().Be("TestValue");
    }

    /// <summary>
    /// Проверяет создание выражения для свойства типа DateTime.
    /// </summary>
    [Fact]
    public void GetPropExpression_DateTimeProperty_CreatesCorrectExpression()
    {
        // Arrange
        const string propertyName = "DateTimeProperty";
        var expectedDate = new DateTime(2023, 12, 25, 14, 30, 0);
        var testObject = new TestObject { DateTimeProperty = expectedDate };

        // Act
        var expression = ExpressionHelper.GetPropExpression<TestObject>(propertyName);
        var compiledFunc = expression.Compile();
        var result = compiledFunc(testObject);

        // Assert
        result.Should().Be(expectedDate);
    }

    /// <summary>
    /// Проверяет создание выражения для вложенного свойства через разделитель.
    /// </summary>
    [Fact]
    public void GetPropExpression_NestedProperty_WithDotDelimiter_CreatesCorrectExpression()
    {
        // Arrange
        const string propertyName = "Nested.Value";
        var testObject = new TestObject
        {
            Nested = new NestedObject { Value = 100 }
        };

        // Act
        var expression = ExpressionHelper.GetPropExpression<TestObject>(propertyName);
        var compiledFunc = expression.Compile();
        var result = compiledFunc(testObject);

        // Assert
        result.Should().Be(100);
    }

    /// <summary>
    /// Проверяет создание выражения для вложенного строкового свойства.
    /// </summary>
    [Fact]
    public void GetPropExpression_NestedStringProperty_WithDotDelimiter_CreatesCorrectExpression()
    {
        // Arrange
        const string propertyName = "Nested.Text";
        var testObject = new TestObject
        {
            Nested = new NestedObject { Text = "NestedText" }
        };

        // Act
        var expression = ExpressionHelper.GetPropExpression<TestObject>(propertyName);
        var compiledFunc = expression.Compile();
        var result = compiledFunc(testObject);

        // Assert
        result.Should().Be("NestedText");
    }

    /// <summary>
    /// Проверяет использование кастомного разделителя вместо точки.
    /// </summary>
    [Fact]
    public void GetPropExpression_NestedProperty_WithCustomDelimiter_CreatesCorrectExpression()
    {
        // Arrange
        const string propertyName = "Nested>Value";
        const char delimiter = '>';
        var testObject = new TestObject
        {
            Nested = new NestedObject { Value = 200 }
        };

        // Act
        var expression = ExpressionHelper.GetPropExpression<TestObject>(propertyName, delimiter);
        var compiledFunc = expression.Compile();
        var result = compiledFunc(testObject);

        // Assert
        result.Should().Be(200);
    }

    /// <summary>
    /// Проверяет, что доступ к свойству через выражение на null-объекте
    /// выбрасывает <see cref="NullReferenceException"/>, так как выражение
    /// не содержит проверки на null.
    /// </summary>
    [Fact]
    public void GetPropExpression_NullObject_AccessingProperty_ThrowsNullReferenceException()
    {
        // Arrange
        const string propertyName = "StringProperty";
        TestObject? testObject = null;

        // Act
        var expression = ExpressionHelper.GetPropExpression<TestObject>(propertyName);
        var compiledFunc = expression.Compile();
        var act = () => compiledFunc(testObject!);

        // Assert
        act.Should().Throw<NullReferenceException>();
    }

    /// <summary>
    /// Проверяет, что созданное выражение имеет правильный тип возврата (object).
    /// </summary>
    [Fact]
    public void GetPropExpression_ReturnType_IsObject()
    {
        // Arrange
        const string propertyName = "IntProperty";

        // Act
        var expression = ExpressionHelper.GetPropExpression<TestObject>(propertyName);

        // Assert
        expression.Type.Should().Be(typeof(Func<TestObject, object>));
        expression.ReturnType.Should().Be(typeof(object));
    }

    /// <summary>
    /// Проверяет работу с глубоко вложенными свойствами (три уровня).
    /// </summary>
    [Fact]
    public void GetPropExpression_DeeplyNestedProperty_CreatesCorrectExpression()
    {
        // Arrange - создаем класс с тремя уровнями вложенности
        var deepNested = new DeepNestedObject
        {
            Level1 = new Level1Object
            {
                Level2 = new Level2Object
                {
                    FinalValue = 999
                }
            }
        };

        // Act
        var expression = ExpressionHelper.GetPropExpression<DeepNestedObject>("Level1.Level2.FinalValue");
        var compiledFunc = expression.Compile();
        var result = compiledFunc(deepNested);

        // Assert
        result.Should().Be(999);
    }

    /// <summary>
    /// Проверяет, что передача несуществующего имени свойства вызывает <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void GetPropExpression_NonExistentProperty_ThrowsArgumentException()
    {
        // Arrange
        const string propertyName = "NonExistent";

        // Act
        var act = () => ExpressionHelper.GetPropExpression<TestObject>(propertyName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*NonExistent*");
    }

    private class DeepNestedObject
    {
        public Level1Object? Level1 { get; set; }
    }

    private class Level1Object
    {
        public Level2Object? Level2 { get; set; }
    }

    private class Level2Object
    {
        public int FinalValue { get; set; }
    }
}
