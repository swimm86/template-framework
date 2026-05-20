using Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions.Queries.Requests;

/// <summary>
/// Тесты для <see cref="ReadByKeyQuery{TResponse}"/>: конструктор, допустимость null, read-only свойство.
/// </summary>
public sealed class ReadByKeyQueryTests
{
    /// <summary>
    /// Конструктор присваивает переданный ключ свойству <see cref="ReadByKeyQuery{TKey}.Key"/>.
    /// </summary>
    [Fact]
    public void Constructor_AssignsKey()
    {
        // Arrange
        var query = new TestReadByKeyQuery("key-123");

        // Act & Assert
        query.Key.Should().Be("key-123");
    }

    /// <summary>
    /// Передача <see langword="null"/> в конструктор не вызывает исключения.
    /// </summary>
    [Fact]
    public void Constructor_NullKey_DoesNotThrow()
    {
        // Arrange
        var query = new TestReadByKeyQuery(null!);

        // Act & Assert
        query.Key.Should().BeNull();
    }

    /// <summary>
    /// Свойство <see cref="ReadByKeyQuery{TKey}.Key"/> доступно только для чтения.
    /// </summary>
    [Fact]
    public void Key_IsGettableProperty()
    {
        // Act
        var property = typeof(TestReadByKeyQuery).GetProperty("Key");

        // Assert
        property.Should().NotBeNull();
        property!.CanRead.Should().BeTrue();
        property.CanWrite.Should().BeFalse();
    }
}
