// ----------------------------------------------------------------------------------------------
// <copyright file="ExpressionExtensionsTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Common.Extensions;

namespace Shared.Common.Tests.Extensions;

/// <summary>
/// Тесты для класса расширения выражений <see cref="ExpressionExtensions"/>.
/// </summary>
public sealed class ExpressionExtensionsTests
{
    #region Test Types

    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public TestEntity? Child { get; set; }
    }

    #endregion

    #region GetPropertyName Tests

    /// <summary>
    /// Проверяет получение имени свойства из выражения.
    /// </summary>
    [Fact]
    public void GetPropertyName_ValidExpression_ReturnsPropertyName()
    {
        // Arrange
        Expression<Func<TestEntity, int>> expression = e => e.Id;

        // Act
        var result = expression.GetPropertyName();

        // Assert
        result.Should().Be("Id");
    }

    /// <summary>
    /// Проверяет получение имени свойства через UnaryExpression (например, приведение типов).
    /// </summary>
    [Fact]
    public void GetPropertyName_UnaryExpression_ReturnsPropertyName()
    {
        // Arrange
        Expression<Func<TestEntity, object>> expression = e => e.Name;

        // Act
        var result = expression.GetPropertyName();

        // Assert
        result.Should().Be("Name");
    }

    #endregion

    #region GetPropertyAccessAndType Tests

    /// <summary>
    /// Проверяет получение выражения доступа и типа для простого свойства.
    /// </summary>
    [Fact]
    public void GetPropertyAccessAndType_ValidPath_ReturnsAccessAndType()
    {
        // Arrange
        var parameter = Expression.Parameter(typeof(TestEntity), "entity");

        // Act
        var (accessExpr, propertyType) = parameter.GetPropertyAccessAndType<TestEntity>(nameof(TestEntity.Name));

        // Assert
        accessExpr.Should().NotBeNull();
        accessExpr!.Type.Should().Be(typeof(string));
        propertyType.Should().Be(typeof(string));
    }

    /// <summary>
    /// Проверяет получение выражения доступа и типа для вложенного свойства.
    /// </summary>
    [Fact]
    public void GetPropertyAccessAndType_NestedPath_ReturnsNestedAccessAndType()
    {
        // Arrange
        var parameter = Expression.Parameter(typeof(TestEntity), "entity");

        // Act
        var (accessExpr, propertyType) = parameter.GetPropertyAccessAndType<TestEntity>($"{nameof(TestEntity.Child)}.{nameof(TestEntity.Id)}");

        // Assert
        accessExpr.Should().NotBeNull();
        propertyType.Should().Be(typeof(int));
    }

    /// <summary>
    /// Проверяет, что при null-параметре возвращаются null-значения.
    /// </summary>
    [Fact]
    public void GetPropertyAccessAndType_NullParameter_ReturnsNulls()
    {
        // Arrange
        ParameterExpression? parameter = null;

        // Act
        var (accessExpr, propertyType) = parameter.GetPropertyAccessAndType<TestEntity>("Name");

        // Assert
        accessExpr.Should().BeNull();
        propertyType.Should().BeNull();
    }

    /// <summary>
    /// Проверяет, что при неверном пути возвращаются null-значения.
    /// </summary>
    [Fact]
    public void GetPropertyAccessAndType_InvalidPath_ReturnsNulls()
    {
        // Arrange
        var parameter = Expression.Parameter(typeof(TestEntity), "entity");

        // Act
        var (accessExpr, propertyType) = parameter.GetPropertyAccessAndType<TestEntity>("NonExistent");

        // Assert
        accessExpr.Should().BeNull();
        propertyType.Should().BeNull();
    }

    #endregion

    #region And Tests

    /// <summary>
    /// Проверяет комбинацию двух выражений через AND — оба условия выполняются.
    /// </summary>
    [Fact]
    public void And_BothConditionsTrue_ReturnsTrue()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expr1 = e => e.Age >= 18;
        Expression<Func<TestEntity, bool>> expr2 = e => e.Age <= 65;
        var combined = expr1.And(expr2).Compile();
        var entity = new TestEntity { Age = 30 };

        // Act
        var result = combined(entity);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет комбинацию двух выражений через AND — первое условие ложно.
    /// </summary>
    [Fact]
    public void And_FirstConditionFalse_ReturnsFalse()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expr1 = e => e.Age >= 18;
        Expression<Func<TestEntity, bool>> expr2 = e => e.Age <= 65;
        var combined = expr1.And(expr2).Compile();
        var entity = new TestEntity { Age = 10 };

        // Act
        var result = combined(entity);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Проверяет комбинацию двух выражений через AND — второе условие ложно.
    /// </summary>
    [Fact]
    public void And_SecondConditionFalse_ReturnsFalse()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expr1 = e => e.Age >= 18;
        Expression<Func<TestEntity, bool>> expr2 = e => e.Age <= 65;
        var combined = expr1.And(expr2).Compile();
        var entity = new TestEntity { Age = 70 };

        // Act
        var result = combined(entity);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что And выбрасывает <see cref="ArgumentNullException"/> при null-первом выражении.
    /// </summary>
    [Fact]
    public void And_NullFirstExpression_ThrowsArgumentNullException()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expr1 = null!;
        Expression<Func<TestEntity, bool>> expr2 = e => e.Age >= 18;

        // Act
        var act = () => expr1.And(expr2);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Проверяет, что And выбрасывает <see cref="ArgumentNullException"/> при null-втором выражении.
    /// </summary>
    [Fact]
    public void And_NullSecondExpression_ThrowsArgumentNullException()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expr1 = e => e.Age >= 18;
        Expression<Func<TestEntity, bool>> expr2 = null!;

        // Act
        var act = () => expr1.And(expr2);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Or Tests

    /// <summary>
    /// Проверяет комбинацию двух выражений через OR — оба условия выполняются.
    /// </summary>
    [Fact]
    public void Or_BothConditionsTrue_ReturnsTrue()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expr1 = e => e.Age >= 18;
        Expression<Func<TestEntity, bool>> expr2 = e => e.Age <= 65;
        var combined = expr1.Or(expr2).Compile();
        var entity = new TestEntity { Age = 30 };

        // Act
        var result = combined(entity);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет комбинацию двух выражений через OR — первое условие истинно.
    /// </summary>
    [Fact]
    public void Or_FirstConditionTrue_ReturnsTrue()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expr1 = e => e.Age >= 18;
        Expression<Func<TestEntity, bool>> expr2 = e => e.Age <= 10;
        var combined = expr1.Or(expr2).Compile();
        var entity = new TestEntity { Age = 30 };

        // Act
        var result = combined(entity);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет комбинацию двух выражений через OR — оба условия ложны.
    /// </summary>
    [Fact]
    public void Or_BothConditionsFalse_ReturnsFalse()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expr1 = e => e.Age >= 18;
        Expression<Func<TestEntity, bool>> expr2 = e => e.Name == "John";
        var combined = expr1.Or(expr2).Compile();
        var entity = new TestEntity { Age = 10, Name = "Jane" };

        // Act
        var result = combined(entity);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что Or выбрасывает <see cref="ArgumentNullException"/> при null-первом выражении.
    /// </summary>
    [Fact]
    public void Or_NullFirstExpression_ThrowsArgumentNullException()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expr1 = null!;
        Expression<Func<TestEntity, bool>> expr2 = e => e.Age >= 18;

        // Act
        var act = () => expr1.Or(expr2);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion
}
