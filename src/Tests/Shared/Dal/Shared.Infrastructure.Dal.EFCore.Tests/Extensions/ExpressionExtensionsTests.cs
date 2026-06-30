using System.Linq.Expressions;
using Shared.Infrastructure.Dal.EFCore.Extensions;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Extensions;

/// <summary>
/// Тесты для <see cref="Shared.Infrastructure.Dal.EFCore.Extensions.ExpressionExtensions.WrapWithCollate{TEntity}"/>.
/// Проверяет преобразование дерева выражений: обёртывание строковых свойств
/// в вызов EF.Functions.Collate и сохранение не-строковых свойств без изменений.
/// </summary>
public sealed class ExpressionExtensionsTests
{
    #region Guard condition tests

    /// <summary>Проверяет что null выражение вызывает ArgumentNullException.</summary>
    [Fact]
    public void WrapWithCollate_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        Expression<Func<TestEntityWithCreatedDeleted, object>> expression = null!;

        // Act
        var act = () => expression.WrapWithCollate("NOCASE");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("expression");
    }

    /// <summary>Проверяет что пустая строка collation вызывает ArgumentException.</summary>
    [Fact]
    public void WrapWithCollate_EmptyCollation_ThrowsArgumentException()
    {
        // Arrange
        Expression<Func<TestEntityWithCreatedDeleted, object>> expression = e => e.Name;

        // Act
        var act = () => expression.WrapWithCollate(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("collation");
    }

    /// <summary>Проверяет что null collation вызывает ArgumentException.</summary>
    [Fact]
    public void WrapWithCollate_NullCollation_ThrowsArgumentException()
    {
        // Arrange
        Expression<Func<TestEntityWithCreatedDeleted, object>> expression = e => e.Name;

        // Act
        var act = () => expression.WrapWithCollate(null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("collation");
    }

    #endregion

    #region String property transformation

    /// <summary>
    /// Проверяет что строковое свойство оборачивается в вызов Collate.
    /// Поскольку выражение возвращает object, компилятор добавляет Convert (boxing),
    /// поэтому реальное тело: Convert(Collate(EF.Functions, e.Name, collation), object).
    /// Тест разворачивает Convert и проверяет MethodCallExpression.
    /// </summary>
    [Fact]
    public void WrapWithCollate_StringProperty_BodyBecomesCollateMethodCall()
    {
        // Arrange
        Expression<Func<TestEntityWithCreatedDeleted, object>> expression = e => e.Name;

        // Act
        var result = expression.WrapWithCollate("NOCASE");

        // Assert
        result.Should().NotBeNull();

        // Unwrap Convert boxing that C# adds when assigning string to object
        var innerBody = result.Body is UnaryExpression { NodeType: ExpressionType.Convert } convert
            ? convert.Operand
            : result.Body;

        // Use BeAssignableTo because EF Core uses an internal MethodCallExpression subclass
        innerBody.Should().BeAssignableTo<MethodCallExpression>();
        var call = (MethodCallExpression)innerBody;
        call.Method.Name.Should().Be("Collate");
    }

    /// <summary>
    /// Проверяет что имя колляции передаётся как константный аргумент в вызов Collate.
    /// </summary>
    [Fact]
    public void WrapWithCollate_StringProperty_CollationPassedAsArgument()
    {
        // Arrange
        const string collation = "NOCASE";
        Expression<Func<TestEntityWithCreatedDeleted, object>> expression = e => e.Name;

        // Act
        var result = expression.WrapWithCollate(collation);

        // Assert — unwrap boxing Convert if present
        var innerBody = result.Body is UnaryExpression { NodeType: ExpressionType.Convert } convert
            ? convert.Operand
            : result.Body;

        var call = (MethodCallExpression)innerBody;
        var collationArg = call.Arguments.OfType<ConstantExpression>()
            .FirstOrDefault(a => a.Value is string);
        collationArg.Should().NotBeNull();
        collationArg.Value.Should().Be(collation);
    }

    /// <summary>
    /// Проверяет что результирующее выражение сохраняет те же параметры, что и исходное.
    /// </summary>
    [Fact]
    public void WrapWithCollate_StringProperty_PreservesParameters()
    {
        // Arrange
        Expression<Func<TestEntityWithCreatedDeleted, object>> expression = e => e.Name;

        // Act
        var result = expression.WrapWithCollate("NOCASE");

        // Assert
        result.Parameters.Should().HaveCount(1);
        result.Parameters[0].Name.Should().Be(expression.Parameters[0].Name);
        result.Parameters[0].Type.Should().Be(expression.Parameters[0].Type);
    }

    /// <summary>
    /// Проверяет что результирующее выражение компилируется без ошибок.
    /// </summary>
    [Fact]
    public void WrapWithCollate_StringProperty_CompilesSuccessfully()
    {
        // Arrange
        Expression<Func<TestEntityWithCreatedDeleted, object>> expression = e => e.Name;

        // Act
        var result = expression.WrapWithCollate("NOCASE");

        // Assert — expression can be compiled (no structural errors)
        var act = () => result.Compile();
        act.Should().NotThrow();
    }

    /// <summary>
    /// Проверяет что разные значения collation передаются корректно.
    /// </summary>
    [Theory]
    [InlineData("NOCASE")]
    [InlineData("BINARY")]
    [InlineData("RTRIM")]
    [InlineData("Latin1_General_CI_AS")]
    public void WrapWithCollate_VariousCollations_CollationArgumentMatches(string collation)
    {
        // Arrange
        Expression<Func<TestEntityWithCreatedDeleted, object>> expression = e => e.Name;

        // Act
        var result = expression.WrapWithCollate(collation);

        // Assert — unwrap boxing Convert if present
        var innerBody = result.Body is UnaryExpression { NodeType: ExpressionType.Convert } convert
            ? convert.Operand
            : result.Body;

        var call = (MethodCallExpression)innerBody;
        var collationArg = call.Arguments.OfType<ConstantExpression>()
            .First(a => a.Value is string s && s == collation);
        collationArg.Value.Should().Be(collation);
    }

    #endregion

    #region Non-string property — no transformation

    /// <summary>
    /// Проверяет что не-строковое свойство (Guid Id) НЕ оборачивается в Collate,
    /// тело выражения остаётся MemberExpression или UnaryExpression (boxing).
    /// </summary>
    [Fact]
    public void WrapWithCollate_NonStringProperty_BodyNotWrappedInCollate()
    {
        // Arrange — Id is Guid (not string), boxing cast to object
        Expression<Func<TestEntityWithCreatedDeleted, object>> expression = e => e.Id;

        // Act
        var result = expression.WrapWithCollate("NOCASE");

        // Assert — body should NOT be a Collate method call
        result.Should().NotBeNull();
        if (result.Body is MethodCallExpression methodCall)
        {
            methodCall.Method.Name.Should().NotBe("Collate");
        }

        // The parameter is the same
        result.Parameters.Should().HaveCount(1);
    }

    #endregion
}
