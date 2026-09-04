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

    #region CombineAllAndAlso Tests

    /// <summary>
    /// Проверяет, что метод возвращает <see langword="null"/> для пустой коллекции.
    /// </summary>
    [Fact]
    public void CombineAllAndAlso_EmptyCollection_ReturnsNull()
    {
        // Arrange
        IReadOnlyList<Expression<Func<TestEntity, bool>>> predicates = [];

        // Act
        var result = predicates.CombineAllAndAlso();

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Проверяет, что метод возвращает <see langword="null"/>, если передана null-коллекция.
    /// </summary>
    [Fact]
    public void CombineAllAndAlso_NullCollection_ReturnsNull()
    {
        // Arrange
        IReadOnlyList<Expression<Func<TestEntity, bool>>>? predicates = null;

        // Act
        var result = predicates.CombineAllAndAlso();

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Проверяет, что для единственного предиката метод возвращает тот же экземпляр без оборачивания.
    /// </summary>
    [Fact]
    public void CombineAllAndAlso_SinglePredicate_ReturnsSameInstance()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Age > 0;
        var predicates = new[] { predicate };

        // Act
        var result = predicates.CombineAllAndAlso();

        // Assert
        result.Should().BeSameAs(predicate);
    }

    /// <summary>
    /// Проверяет, что результат семантически эквивалентен логическому "И" двух предикатов
    /// в случае, когда оба условия выполняются.
    /// </summary>
    [Fact]
    public void CombineAllAndAlso_TwoPredicates_BothTrue_ReturnsTrue()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> first = e => e.Age >= 18;
        Expression<Func<TestEntity, bool>> second = e => e.Age <= 65;
        var predicates = new[] { first, second };
        var entity = new TestEntity { Age = 30 };

        // Act
        var combined = predicates.CombineAllAndAlso()!.Compile();

        // Assert
        combined(entity).Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что результат соответствует логическому "И" (а не "ИЛИ"):
    /// если первый предикат истинен, а второй ложен, результат должен быть ложным.
    /// Выявляет баг замены AndAlso на OrElse.
    /// </summary>
    [Fact]
    public void CombineAllAndAlso_TwoPredicates_FirstTrueSecondFalse_ReturnsFalse()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> first = e => e.Age > 0;
        Expression<Func<TestEntity, bool>> second = e => e.Age < 0;
        var predicates = new[] { first, second };
        var entity = new TestEntity { Age = 30 };

        // Act
        var combined = predicates.CombineAllAndAlso()!.Compile();

        // Assert
        combined(entity).Should().BeFalse("AND requires all operands to be true");
    }

    /// <summary>
    /// Проверяет, что результат соответствует логическому "И":
    /// если первый предикат ложен, а второй истинен, результат должен быть ложным.
    /// </summary>
    [Fact]
    public void CombineAllAndAlso_TwoPredicates_FirstFalseSecondTrue_ReturnsFalse()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> first = e => e.Age < 0;
        Expression<Func<TestEntity, bool>> second = e => e.Age > 0;
        var predicates = new[] { first, second };
        var entity = new TestEntity { Age = 30 };

        // Act
        var combined = predicates.CombineAllAndAlso()!.Compile();

        // Assert
        combined(entity).Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что для трёх предикатов, где отвергает последний, результат ложен.
    /// Выявляет баг «метод учитывает только первые два предиката».
    /// </summary>
    [Fact]
    public void CombineAllAndAlso_ThreePredicates_OnlyLastFalse_ReturnsFalse()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> first = e => e.Age > 0;
        Expression<Func<TestEntity, bool>> middle = e => e.Age < 100;
        Expression<Func<TestEntity, bool>> last = e => e.Age != 30;
        var predicates = new[] { first, middle, last };
        var entity = new TestEntity { Age = 30 };

        // Act
        var combined = predicates.CombineAllAndAlso()!.Compile();

        // Assert
        combined(entity).Should().BeFalse("all three predicates must be AND-ed");
    }

    /// <summary>
    /// Проверяет, что для трёх предикатов, где отвергает средний, результат ложен.
    /// Выявляет баг «метод применяет только первый и последний предикаты».
    /// </summary>
    [Fact]
    public void CombineAllAndAlso_ThreePredicates_OnlyMiddleFalse_ReturnsFalse()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> first = e => e.Age > 0;
        Expression<Func<TestEntity, bool>> middle = e => e.Age == 0;
        Expression<Func<TestEntity, bool>> last = e => e.Age < 100;
        var predicates = new[] { first, middle, last };
        var entity = new TestEntity { Age = 30 };

        // Act
        var combined = predicates.CombineAllAndAlso()!.Compile();

        // Assert
        combined(entity).Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что для трёх предикатов, где все истинны, результат истинен.
    /// </summary>
    [Fact]
    public void CombineAllAndAlso_ThreePredicates_AllTrue_ReturnsTrue()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> first = e => e.Age > 0;
        Expression<Func<TestEntity, bool>> middle = e => e.Age == 30;
        Expression<Func<TestEntity, bool>> last = e => e.Age < 100;
        var predicates = new[] { first, middle, last };
        var entity = new TestEntity { Age = 30 };

        // Act
        var combined = predicates.CombineAllAndAlso()!.Compile();

        // Assert
        combined(entity).Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что предикаты, имеющие разные экземпляры <see cref="ParameterExpression"/>,
    /// корректно унифицируются и компилируются без исключений.
    /// Выявляет баг «параметры выражений не переименовываются при объединении».
    /// </summary>
    /// <param name="age">Возраст сущности.</param>
    /// <param name="expected">Ожидаемый результат комбинированного предиката.</param>
    [Theory]
    [InlineData(30, true)]
    [InlineData(-1, false)]
    [InlineData(200, false)]
    public void CombineAllAndAlso_DifferentParameterInstances_CompilesAndExecutesCorrectly(int age, bool expected)
    {
        // Arrange
        var firstParameter = Expression.Parameter(typeof(TestEntity), "alpha");
        var secondParameter = Expression.Parameter(typeof(TestEntity), "beta");
        Expression<Func<TestEntity, bool>> first =
            Expression.Lambda<Func<TestEntity, bool>>(
                Expression.GreaterThan(
                    Expression.Property(firstParameter, nameof(TestEntity.Age)),
                    Expression.Constant(0)),
                firstParameter);
        Expression<Func<TestEntity, bool>> second =
            Expression.Lambda<Func<TestEntity, bool>>(
                Expression.LessThan(
                    Expression.Property(secondParameter, nameof(TestEntity.Age)),
                    Expression.Constant(100)),
                secondParameter);
        var predicates = new[] { first, second };
        var entity = new TestEntity { Age = age };

        // Act
        var combined = predicates.CombineAllAndAlso()!.Compile();

        // Assert
        combined(entity).Should().Be(expected);
    }

    /// <summary>
    /// Проверяет, что исходные предикаты остаются работоспособными после вызова метода.
    /// Выявляет баг «метод мутирует входные выражения».
    /// </summary>
    [Fact]
    public void CombineAllAndAlso_OriginalPredicatesRemainUnchanged()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> first = e => e.Age > 0;
        Expression<Func<TestEntity, bool>> second = e => e.Age < 100;
        var predicates = new[] { first, second };
        var entity = new TestEntity { Age = 30 };

        // Act
        _ = predicates.CombineAllAndAlso();

        // Assert
        first.Compile()(entity).Should().BeTrue();
        second.Compile()(entity).Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что метод выбрасывает <see cref="ArgumentException"/>, если коллекция содержит null.
    /// Выявляет баг «метод не валидирует элементы и падает с NullReferenceException».
    /// </summary>
    [Fact]
    public void CombineAllAndAlso_CollectionContainsNull_ThrowsArgumentException()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> valid = e => e.Age > 0;
        var predicates = new Expression<Func<TestEntity, bool>>[] { valid, null! };

        // Act
        var act = () => predicates.CombineAllAndAlso();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Проверяет, что метод выбрасывает <see cref="ArgumentException"/>, если null-элемент
    /// находится в начале коллекции.
    /// </summary>
    [Fact]
    public void CombineAllAndAlso_NullAtFirstIndex_ThrowsArgumentException()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> valid = e => e.Age > 0;
        var predicates = new Expression<Func<TestEntity, bool>>[] { null!, valid };

        // Act
        var act = () => predicates.CombineAllAndAlso();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Проверяет, что метод выбрасывает <see cref="ArgumentException"/>,
    /// если единственный элемент коллекции равен <see langword="null"/>.
    /// Выявляет баг «метод возвращает null без диагностики для коллекции с одним null-элементом».
    /// </summary>
    [Fact]
    public void CombineAllAndAlso_SingleNullElement_ThrowsArgumentException()
    {
        // Arrange
        Expression<Func<TestEntity, bool>>[] predicates = [null!];

        // Act
        var act = () => predicates.CombineAllAndAlso();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Проверяет, что предикаты с захваченными переменными
    /// корректно объединяются и могут компилироваться/выполняться.
    /// </summary>
    [Fact]
    public void CombineAllAndAlso_PredicatesWithCapturedVariables_AllTrue_ReturnsTrue()
    {
        // Arrange
        var minAge = 18;
        var maxAge = 65;
        var targetName = "John";
        Expression<Func<TestEntity, bool>> ageRange = e => e.Age >= minAge && e.Age <= maxAge;
        Expression<Func<TestEntity, bool>> nameMatch = e => e.Name == targetName;
        var predicates = new[] { ageRange, nameMatch };
        var entityJohn30 = new TestEntity { Age = 30, Name = "John" };

        // Act
        var combined = predicates.CombineAllAndAlso()!.Compile();

        // Assert
        combined(entityJohn30).Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что при захваченных переменных комбинированный предикат возвращает <c>false</c>,
    /// если хотя бы один предикат отвергает сущность.
    /// </summary>
    [Fact]
    public void CombineAllAndAlso_PredicatesWithCapturedVariables_SecondFalse_ReturnsFalse()
    {
        // Arrange
        var minAge = 18;
        var maxAge = 65;
        var targetName = "John";
        Expression<Func<TestEntity, bool>> ageRange = e => e.Age >= minAge && e.Age <= maxAge;
        Expression<Func<TestEntity, bool>> nameMatch = e => e.Name == targetName;
        var predicates = new[] { ageRange, nameMatch };
        var entityJane30 = new TestEntity { Age = 30, Name = "Jane" };

        // Act
        var combined = predicates.CombineAllAndAlso()!.Compile();

        // Assert
        combined(entityJane30).Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что предикаты с захваченными переменными
    /// могут быть объединены и скомпилированы для выражения значительной глубины без исключений.
    /// </summary>
    [Fact]
    public void CombineAllAndAlso_ManyPredicatesWithCapturedVariables_CompilesSuccessfully()
    {
        // Arrange
        var predicates = Enumerable.Range(0, 64)
            .Select(i =>
            {
                var captured = i;
                return (Expression<Func<TestEntity, bool>>)(e => e.Id != captured);
            })
            .ToList();

        // Act
        var compiled = predicates.CombineAllAndAlso()!.Compile();

        // Assert
        compiled(new TestEntity { Id = -1 }).Should().BeTrue();
    }

    #endregion

    #region CombineAllOrElse Tests

    /// <summary>
    /// Проверяет, что метод возвращает <see langword="null"/> для пустой коллекции.
    /// </summary>
    [Fact]
    public void CombineAllOrElse_EmptyCollection_ReturnsNull()
    {
        // Arrange
        IReadOnlyList<Expression<Func<TestEntity, bool>>> predicates = [];

        // Act
        var result = predicates.CombineAllOrElse();

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Проверяет, что метод возвращает <see langword="null"/>, если передана null-коллекция.
    /// </summary>
    [Fact]
    public void CombineAllOrElse_NullCollection_ReturnsNull()
    {
        // Arrange
        IReadOnlyList<Expression<Func<TestEntity, bool>>>? predicates = null;

        // Act
        var result = predicates.CombineAllOrElse();

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Проверяет, что для единственного предиката метод возвращает тот же экземпляр без оборачивания.
    /// </summary>
    [Fact]
    public void CombineAllOrElse_SinglePredicate_ReturnsSameInstance()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Age > 0;
        var predicates = new[] { predicate };

        // Act
        var result = predicates.CombineAllOrElse();

        // Assert
        result.Should().BeSameAs(predicate);
    }

    /// <summary>
    /// Проверяет, что результат соответствует логическому «ИЛИ»: оба условия ложны → результат ложен.
    /// </summary>
    [Fact]
    public void CombineAllOrElse_TwoPredicates_BothFalse_ReturnsFalse()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> first = e => e.Age > 100;
        Expression<Func<TestEntity, bool>> second = e => e.Age < 0;
        var predicates = new[] { first, second };
        var entity = new TestEntity { Age = 30 };

        // Act
        var combined = predicates.CombineAllOrElse()!.Compile();

        // Assert
        combined(entity).Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что результат соответствует логическому «ИЛИ»: первый истинен, второй ложен → результат истинен.
    /// Выявляет баг замены OrElse на AndAlso.
    /// </summary>
    [Fact]
    public void CombineAllOrElse_TwoPredicates_FirstTrueSecondFalse_ReturnsTrue()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> first = e => e.Age > 0;
        Expression<Func<TestEntity, bool>> second = e => e.Age < 0;
        var predicates = new[] { first, second };
        var entity = new TestEntity { Age = 30 };

        // Act
        var combined = predicates.CombineAllOrElse()!.Compile();

        // Assert
        combined(entity).Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что результат соответствует логическому «ИЛИ»: первый ложен, второй истинен → результат истинен.
    /// </summary>
    [Fact]
    public void CombineAllOrElse_TwoPredicates_FirstFalseSecondTrue_ReturnsTrue()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> first = e => e.Age < 0;
        Expression<Func<TestEntity, bool>> second = e => e.Age > 0;
        var predicates = new[] { first, second };
        var entity = new TestEntity { Age = 30 };

        // Act
        var combined = predicates.CombineAllOrElse()!.Compile();

        // Assert
        combined(entity).Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что для трёх предикатов, где все ложны, результат ложен.
    /// </summary>
    [Fact]
    public void CombineAllOrElse_ThreePredicates_AllFalse_ReturnsFalse()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> first = e => e.Age < 0;
        Expression<Func<TestEntity, bool>> middle = e => e.Age > 100;
        Expression<Func<TestEntity, bool>> last = e => e.Age == 999;
        var predicates = new[] { first, middle, last };
        var entity = new TestEntity { Age = 30 };

        // Act
        var combined = predicates.CombineAllOrElse()!.Compile();

        // Assert
        combined(entity).Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что для трёх предикатов, где хотя бы один истинен, результат истинен.
    /// </summary>
    [Fact]
    public void CombineAllOrElse_ThreePredicates_OnlyMiddleTrue_ReturnsTrue()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> first = e => e.Age < 0;
        Expression<Func<TestEntity, bool>> middle = e => e.Age == 30;
        Expression<Func<TestEntity, bool>> last = e => e.Age > 100;
        var predicates = new[] { first, middle, last };
        var entity = new TestEntity { Age = 30 };

        // Act
        var combined = predicates.CombineAllOrElse()!.Compile();

        // Assert
        combined(entity).Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что метод выбрасывает <see cref="ArgumentException"/>, если коллекция содержит null.
    /// </summary>
    [Fact]
    public void CombineAllOrElse_CollectionContainsNull_ThrowsArgumentException()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> valid = e => e.Age > 0;
        var predicates = new Expression<Func<TestEntity, bool>>[] { valid, null! };

        // Act
        var act = () => predicates.CombineAllOrElse();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Проверяет, что единственный null-элемент в коллекции приводит к <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void CombineAllOrElse_SingleNullElement_ThrowsArgumentException()
    {
        // Arrange
        Expression<Func<TestEntity, bool>>[] predicates = [null!];

        // Act
        var act = () => predicates.CombineAllOrElse();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion
}
