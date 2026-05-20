using System.Reflection;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Exceptions.Models;

/// <summary>
/// Модульные тесты для <see cref="NotFoundException"/>.
/// </summary>
public sealed class NotFoundExceptionTests
{
    /// <summary>
    /// Проверяет, что параметр-конструктор <see cref="NotFoundException"/> без параметров
    /// не выбрасывает исключений.
    /// </summary>
    [Fact]
    public void Constructor_Default_DoesNotThrow()
    {
        // Arrange

        // Act
        Action act = () => new NotFoundException();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Проверяет, что <see cref="NotFoundException"/> сохраняет сообщение,
    /// переданное в конструктор.
    /// </summary>
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        const string expected = "not found error";

        // Act
        var exception = new NotFoundException(expected);

        // Assert
        exception.Message.Should().Be(expected);
    }

    /// <summary>
    /// Проверяет, что <see cref="NotFoundException"/> с <see cref="MemberInfo"/>
    /// и ключом использует значение атрибута <see cref="Shared.Domain.Core.Attributes.EntityNameAttribute"/>
    /// в сообщении.
    /// </summary>
    [Fact]
    public void Constructor_WithMemberInfoAndKey_UsesEntityNameAttribute()
    {
        // Arrange
        MemberInfo entityType = typeof(EntityWithEntityName);
        const int key = 42;

        // Act
        var exception = new NotFoundException(entityType, key);

        // Assert
        exception.Message.Should().Contain("Тест");
        exception.Message.Should().Contain("42");
    }

    /// <summary>
    /// Проверяет, что <see cref="NotFoundException"/> с <see cref="MemberInfo"/>
    /// типа без атрибута <see cref="Shared.Domain.Core.Attributes.EntityNameAttribute"/>
    /// использует <see cref="MemberInfo.Name"/> в сообщении.
    /// </summary>
    [Fact]
    public void Constructor_WithMemberInfoAndKey_FallsBackToTypeName()
    {
        // Arrange
        MemberInfo entityType = typeof(EntityWithoutEntityName);
        const int key = 42;

        // Act
        var exception = new NotFoundException(entityType, key);

        // Assert
        exception.Message.Should().Contain(nameof(EntityWithoutEntityName));
    }

    /// <summary>
    /// Проверяет, что <see cref="NotFoundException"/> с массивом ключей
    /// форматирует все ключи в сообщении через запятую.
    /// </summary>
    [Fact]
    public void Constructor_WithMemberInfoAndKeys_FormatsAllKeys()
    {
        // Arrange
        MemberInfo entityType = typeof(EntityWithEntityName);
        object[] keys = { 1, 2, 3 };

        // Act
        var exception = new NotFoundException(entityType, keys);

        // Assert
        exception.Message.Should().Contain("Тест");
        exception.Message.Should().Contain("1, 2, 3");
    }

    /// <summary>
    /// Проверяет, что <see cref="NotFoundException"/> с сообщением и внутренним исключением
    /// сохраняет оба значения.
    /// </summary>
    [Fact]
    public void Constructor_WithMessageAndInnerException_PreservesBoth()
    {
        // Arrange
        var inner = new InvalidOperationException("inner error");

        // Act
        var exception = new NotFoundException("outer message", inner);

        // Assert
        exception.Message.Should().Be("outer message");
        exception.InnerException.Should().Be(inner);
    }
}
